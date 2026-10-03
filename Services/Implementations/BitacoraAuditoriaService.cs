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

            var registros = await query
                .OrderByDescending(b => b.FechaHora)
                .ThenByDescending(b => b.BitacoraAuditoriaId)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
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
            return await _context.BitacoraAuditoria
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
