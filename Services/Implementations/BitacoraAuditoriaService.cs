using HikariLegalSRL.Controllers.BitacoraAuditoria;
using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class BitacoraAuditoriaService : IBitacoraAuditoriaService
    {
        public const int TamanoPagina = 25;
        private const int LongitudResumen = 60;

        // Antes de esta fecha, "Expedientes" mezclaba ExpedienteId y TareaId, y "Prospectos"
        // mezclaba ProspectoId y ActividadSeguimientoId, en el mismo campo RegistroAfectadoId
        // (dos secuencias de autoincremento distintas, con ids que sí se solapan en la práctica:
        // ver .claude/notas/notas.md). Resolver el nombre de esas filas antiguas podría mostrar
        // el nombre equivocado. Desde este id ya se escriben "Tareas" y "Actividades" aparte, así
        // que son inambiguas y sí se resuelven. El resto de módulos nunca fue ambiguo.
        private const long CorteModulosDesambiguados = 20389;

        private static readonly HashSet<string> ModulosAmbiguosAntesDelCorte = new() { "Expedientes", "Prospectos" };

        public static readonly IReadOnlyList<string> TiposAccion = new[]
        {
            "crear", "editar", "eliminar", "cambiar_estado", "aprobar", "rechazar"
        };

        private readonly ApplicationDbContext _context;

        public BitacoraAuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Crea una nueva instancia de BitacoraAuditoria con los valores proporcionados y la persiste en la base de datos
        public async Task Registrar(string usuarioId, string tipoAccion, string moduloAfectado, string registroAfectadoId, string? valorAnterior = null, string? valorNuevo = null)
        {
            var registro = new BitacoraAuditoria
            {
                UsuarioId = usuarioId,
                TipoAccion = tipoAccion,
                ModuloAfectado = moduloAfectado,
                RegistroAfectadoId = registroAfectadoId,
                ValorAnterior = valorAnterior,
                ValorNuevo = valorNuevo,
                FechaHora = DateTime.UtcNow
            };
            _context.BitacoraAuditoria.Add(registro); //Agrega el registro a la base de datos
        }

        public async Task<BitacoraPaginaDTO> Consultar(BitacoraFiltroDTO filtro)
        {
            var query = _context.BitacoraAuditoria.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.UsuarioId))
                query = query.Where(b => b.UsuarioId == filtro.UsuarioId);

            if (!string.IsNullOrWhiteSpace(filtro.TipoAccion) && TiposAccion.Contains(filtro.TipoAccion))
                query = query.Where(b => b.TipoAccion == filtro.TipoAccion);

            if (filtro.Desde.HasValue)
            {
                var desdeUtc = DateTime.SpecifyKind(TruncarAMinuto(filtro.Desde.Value), DateTimeKind.Local).ToUniversalTime();
                query = query.Where(b => b.FechaHora >= desdeUtc);
            }

            if (filtro.Hasta.HasValue)
            {
                var hastaUtc = DateTime.SpecifyKind(TruncarAMinuto(filtro.Hasta.Value).AddMinutes(1), DateTimeKind.Local).ToUniversalTime();
                query = query.Where(b => b.FechaHora < hastaUtc);
            }

            var total = await query.CountAsync();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
            var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);

            var ids = await query
                .OrderByDescending(b => b.FechaHora)
                .ThenByDescending(b => b.BitacoraAuditoriaId)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .Select(b => b.BitacoraAuditoriaId)
                .ToListAsync();

            var registros = await _context.BitacoraAuditoria
                .AsNoTracking()
                .Where(b => ids.Contains(b.BitacoraAuditoriaId))
                .Select(b => new BitacoraRegistroDTO
                {
                    Id = b.BitacoraAuditoriaId,
                    FechaHora = b.FechaHora,
                    UsuarioNombre = b.Usuario.NombreCompleto,
                    TipoAccion = b.TipoAccion,
                    ModuloAfectado = b.ModuloAfectado,
                    RegistroAfectadoId = b.RegistroAfectadoId,
                    ValorAnteriorResumen = b.ValorAnterior == null || b.ValorAnterior.Length <= LongitudResumen
                        ? b.ValorAnterior
                        : b.ValorAnterior.Substring(0, LongitudResumen),
                    ValorAnteriorTieneMas = b.ValorAnterior != null && b.ValorAnterior.Length > LongitudResumen,
                    ValorNuevoResumen = b.ValorNuevo == null || b.ValorNuevo.Length <= LongitudResumen
                        ? b.ValorNuevo
                        : b.ValorNuevo.Substring(0, LongitudResumen),
                    ValorNuevoTieneMas = b.ValorNuevo != null && b.ValorNuevo.Length > LongitudResumen
                })
                .ToListAsync();

            registros = registros.OrderBy(r => ids.IndexOf(r.Id)).ToList();

            var nombres = await ResolverNombresEntidadesAsync(registros.Select(r => (r.Id, r.ModuloAfectado, r.RegistroAfectadoId)));
            foreach (var registro in registros)
            {
                if (EsResolvible(registro.Id, registro.ModuloAfectado)
                    && nombres.TryGetValue((registro.ModuloAfectado, registro.RegistroAfectadoId), out var nombre))
                {
                    registro.EntidadNombre = nombre;
                }
            }

            return new BitacoraPaginaDTO
            {
                Registros = registros,
                Total = total,
                Pagina = pagina,
                TamanoPagina = TamanoPagina
            };
        }

        private static DateTime TruncarAMinuto(DateTime fecha) =>
            new(fecha.Year, fecha.Month, fecha.Day, fecha.Hour, fecha.Minute, 0);

        public async Task<BitacoraDetalleDTO?> ObtenerDetalle(long id)
        {
            var registro = await _context.BitacoraAuditoria
                .AsNoTracking()
                .Where(b => b.BitacoraAuditoriaId == id)
                .Select(b => new BitacoraDetalleDTO
                {
                    Id = b.BitacoraAuditoriaId,
                    FechaHora = b.FechaHora,
                    UsuarioNombre = b.Usuario.NombreCompleto,
                    UsuarioCorreo = b.Usuario.Email,
                    TipoAccion = b.TipoAccion,
                    ModuloAfectado = b.ModuloAfectado,
                    RegistroAfectadoId = b.RegistroAfectadoId,
                    ValorAnterior = b.ValorAnterior,
                    ValorNuevo = b.ValorNuevo
                })
                .FirstOrDefaultAsync();

            if (registro is not null && EsResolvible(registro.Id, registro.ModuloAfectado))
            {
                var nombres = await ResolverNombresEntidadesAsync(new[] { (registro.Id, registro.ModuloAfectado, registro.RegistroAfectadoId) });
                if (nombres.TryGetValue((registro.ModuloAfectado, registro.RegistroAfectadoId), out var nombre))
                    registro.EntidadNombre = nombre;
            }

            return registro;
        }

        // Solo los módulos que antes del corte mezclaban dos tipos de entidad en el mismo
        // RegistroAfectadoId necesitan quedar fuera; el resto siempre fue inambiguo.
        private static bool EsResolvible(long bitacoraId, string moduloAfectado) =>
            !ModulosAmbiguosAntesDelCorte.Contains(moduloAfectado) || bitacoraId > CorteModulosDesambiguados;

        // Resuelve, en lote, el nombre legible de la entidad afectada por cada fila (Id, Módulo,
        // RegistroAfectadoId). Una consulta por módulo presente, nunca una por fila. Las claves
        // que no correspondan a ningún módulo conocido, o cuyo id no exista, simplemente no
        // aparecen en el resultado — el llamador debe tratarlo como "sin nombre", nunca como error.
        private async Task<Dictionary<(string Modulo, string Id), string>> ResolverNombresEntidadesAsync(
            IEnumerable<(long BitacoraId, string Modulo, string Id)> filas)
        {
            var nombres = new Dictionary<(string, string), string>();

            var claves = filas
                .Where(f => EsResolvible(f.BitacoraId, f.Modulo))
                .Select(f => (f.Modulo, f.Id))
                .Distinct()
                .ToList();

            List<int> IdsEnteros(string modulo) => claves
                .Where(c => c.Modulo == modulo)
                .Select(c => int.TryParse(c.Id, out var valor) ? valor : (int?)null)
                .Where(valor => valor.HasValue)
                .Select(valor => valor!.Value)
                .ToList();

            // Siempre se materializa con Select(...) + ToListAsync() antes de armar el diccionario:
            // ToDictionaryAsync(clave, valor) con un valor que cruza una navegación (p.ej.
            // f.Cliente.NombreEmpresaPersona) no lo traduce a SQL y evalúa el selector en memoria
            // sobre la entidad sin esa navegación cargada (AsNoTracking, sin Include), y lanza
            // NullReferenceException. Con Select(...) el cruce si se traduce, como en el resto del
            // servicio (ver Consultar/ObtenerDetalle con b.Usuario.NombreCompleto).
            async Task RegistrarAsync<TId>(string modulo, IQueryable<KeyValuePair<TId, string?>> consulta) where TId : notnull
            {
                var filas = await consulta.ToListAsync();
                foreach (var fila in filas)
                {
                    if (fila.Value is not null)
                        nombres[(modulo, fila.Key.ToString()!)] = fila.Value;
                }
            }

            var idsClientes = IdsEnteros("Clientes");
            if (idsClientes.Count > 0)
                await RegistrarAsync("Clientes", _context.Clientes.AsNoTracking()
                    .Where(c => idsClientes.Contains(c.ClienteId))
                    .Select(c => new KeyValuePair<int, string?>(c.ClienteId, c.NombreEmpresaPersona)));

            var idsProspectos = IdsEnteros("Prospectos");
            if (idsProspectos.Count > 0)
                await RegistrarAsync("Prospectos", _context.Prospectos.AsNoTracking()
                    .Where(p => idsProspectos.Contains(p.ProspectoId))
                    .Select(p => new KeyValuePair<int, string?>(p.ProspectoId, p.NombreEmpresaPersona)));

            var idsActividades = IdsEnteros("Actividades");
            if (idsActividades.Count > 0)
                await RegistrarAsync("Actividades", _context.ActividadesSeguimiento.AsNoTracking()
                    .Where(a => idsActividades.Contains(a.ActividadSeguimientoId))
                    .Select(a => new KeyValuePair<int, string?>(a.ActividadSeguimientoId, a.Titulo)));

            var idsServicios = IdsEnteros("Servicios");
            if (idsServicios.Count > 0)
                await RegistrarAsync("Servicios", _context.CatalogoServicios.AsNoTracking()
                    .Where(s => idsServicios.Contains(s.ServicioId))
                    .Select(s => new KeyValuePair<int, string?>(s.ServicioId, s.Nombre)));

            var idsPropuestas = IdsEnteros("Propuestas");
            if (idsPropuestas.Count > 0)
                await RegistrarAsync("Propuestas", _context.Propuestas.AsNoTracking()
                    .Where(p => idsPropuestas.Contains(p.PropuestaId))
                    .Select(p => new KeyValuePair<int, string?>(p.PropuestaId, p.Cliente != null ? p.Cliente.NombreEmpresaPersona : p.Prospecto!.NombreEmpresaPersona)));

            var idsExpedientes = IdsEnteros("Expedientes");
            if (idsExpedientes.Count > 0)
                await RegistrarAsync("Expedientes", _context.Expedientes.AsNoTracking()
                    .Where(e => idsExpedientes.Contains(e.ExpedienteId))
                    .Select(e => new KeyValuePair<int, string?>(e.ExpedienteId, e.Cliente.NombreEmpresaPersona)));

            var idsCalidad = IdsEnteros("Calidad");
            if (idsCalidad.Count > 0)
                await RegistrarAsync("Calidad", _context.Expedientes.AsNoTracking()
                    .Where(e => idsCalidad.Contains(e.ExpedienteId))
                    .Select(e => new KeyValuePair<int, string?>(e.ExpedienteId, e.Cliente.NombreEmpresaPersona)));

            var idsTareas = IdsEnteros("Tareas");
            if (idsTareas.Count > 0)
                await RegistrarAsync("Tareas", _context.Tareas.AsNoTracking()
                    .Where(t => idsTareas.Contains(t.TareaId))
                    .Select(t => new KeyValuePair<int, string?>(t.TareaId, t.Descripcion)));

            var idsFacturas = IdsEnteros("Facturación");
            if (idsFacturas.Count > 0)
                await RegistrarAsync("Facturación", _context.Facturas.AsNoTracking()
                    .Where(f => idsFacturas.Contains(f.FacturaId))
                    .Select(f => new KeyValuePair<int, string?>(f.FacturaId, f.Cliente.NombreEmpresaPersona)));

            var idsProBono = IdsEnteros("ProBono");
            if (idsProBono.Count > 0)
                await RegistrarAsync("ProBono", _context.SolicitudesProBono.AsNoTracking()
                    .Where(s => idsProBono.Contains(s.SolicitudProBonoId))
                    .Select(s => new KeyValuePair<int, string?>(s.SolicitudProBonoId, s.Cliente != null ? s.Cliente.NombreEmpresaPersona : s.Prospecto!.NombreEmpresaPersona)));

            var idsUsuarios = claves.Where(c => c.Modulo == "Usuarios").Select(c => c.Id).Distinct().ToList();
            if (idsUsuarios.Count > 0)
                await RegistrarAsync("Usuarios", _context.Users.AsNoTracking()
                    .Where(u => idsUsuarios.Contains(u.Id))
                    .Select(u => new KeyValuePair<string, string?>(u.Id, u.NombreCompleto)));

            var idsRoles = claves.Where(c => c.Modulo == "Roles").Select(c => c.Id).Distinct().ToList();
            if (idsRoles.Count > 0)
                await RegistrarAsync("Roles", _context.Roles.AsNoTracking()
                    .Where(r => idsRoles.Contains(r.Id))
                    .Select(r => new KeyValuePair<string, string?>(r.Id, r.Name)));

            return nombres;
        }

        public async Task<List<UsuarioOpcionDTO>> ObtenerUsuariosConRegistros()
        {
            return await _context.BitacoraAuditoria
                .AsNoTracking()
                .Select(b => new UsuarioOpcionDTO { Id = b.UsuarioId, Nombre = b.Usuario.NombreCompleto })
                .Distinct()
                .OrderBy(u => u.Nombre)
                .ToListAsync();
        }
    }
}
