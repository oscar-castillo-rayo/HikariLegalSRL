using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ProBonoService : IProBonoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ProBonoService(
            ApplicationDbContext context,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        // RF-011: el historial completo solo lo consulta quien puede aprobar/rechazar
        // (Administrador, vía facturacion.aprobar_probono); cualquier otro usuario solo ve
        // las solicitudes que él mismo presentó.
        private async Task<bool> PuedeVerTodasAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Facturacion.AprobarProbono);
        }

        public async Task<List<OpcionComboDTO>> ObtenerProspectosActivos()
        {
            return await _context.Prospectos
                .AsNoTracking()
                .Where(p => p.Estado == EstadoProspecto.Activo)
                .OrderBy(p => p.NombreEmpresaPersona)
                .Select(p => new OpcionComboDTO { Id = p.ProspectoId, Nombre = p.NombreEmpresaPersona })
                .ToListAsync();
        }

        public async Task<List<OpcionComboDTO>> ObtenerClientesActivos()
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Estado == EstadoCliente.Activo)
                .OrderBy(c => c.NombreEmpresaPersona)
                .Select(c => new OpcionComboDTO { Id = c.ClienteId, Nombre = c.NombreEmpresaPersona })
                .ToListAsync();
        }

        public async Task<List<SolicitudProBonoListaDTO>> Listar(string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var query = _context.SolicitudesProBono
                .AsNoTracking()
                .Include(s => s.Cliente)
                .Include(s => s.Prospecto)
                .Include(s => s.Solicitante)
                .Include(s => s.ResueltoPor)
                .AsQueryable();

            if (!puedeVerTodas)
                query = query.Where(s => s.SolicitanteId == usuarioActualId);

            return await query
                .OrderByDescending(s => s.FechaSolicitud)
                .Select(s => new SolicitudProBonoListaDTO
                {
                    Id = s.SolicitudProBonoId,
                    Beneficiario = s.Cliente != null ? s.Cliente.NombreEmpresaPersona : s.Prospecto!.NombreEmpresaPersona,
                    SolicitanteNombre = s.Solicitante.NombreCompleto,
                    JustificacionEscrita = s.JustificacionEscrita,
                    Decision = s.Decision,
                    FechaSolicitud = s.FechaSolicitud,
                    ResueltoPorNombre = s.ResueltoPor != null ? s.ResueltoPor.NombreCompleto : null,
                    FechaResolucion = s.FechaResolucion
                })
                .ToListAsync();
        }

        public async Task<SolicitudProBonoDetalleDTO?> ObtenerDetalle(int id, string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var solicitud = await _context.SolicitudesProBono
                .AsNoTracking()
                .Include(s => s.Cliente)
                .Include(s => s.Prospecto)
                .Include(s => s.Solicitante)
                .Include(s => s.ResueltoPor)
                .FirstOrDefaultAsync(s => s.SolicitudProBonoId == id);

            if (solicitud is null)
                return null;

            if (!puedeVerTodas && solicitud.SolicitanteId != usuarioActualId)
                return null;

            return new SolicitudProBonoDetalleDTO
            {
                Id = solicitud.SolicitudProBonoId,
                ClienteId = solicitud.ClienteId,
                ProspectoId = solicitud.ProspectoId,
                Beneficiario = solicitud.Cliente?.NombreEmpresaPersona ?? solicitud.Prospecto!.NombreEmpresaPersona,
                SolicitanteId = solicitud.SolicitanteId,
                SolicitanteNombre = solicitud.Solicitante.NombreCompleto,
                JustificacionEscrita = solicitud.JustificacionEscrita,
                Decision = solicitud.Decision,
                ComentarioResolucion = solicitud.ComentarioResolucion,
                ResueltoPorNombre = solicitud.ResueltoPor?.NombreCompleto,
                FechaSolicitud = solicitud.FechaSolicitud,
                FechaResolucion = solicitud.FechaResolucion
            };
        }

        public async Task<int> Crear(SolicitudProBonoCreacionDTO dto, string usuarioActualId)
        {
            var tieneCliente = dto.ClienteId.HasValue && dto.ClienteId.Value > 0;
            var tieneProspecto = dto.ProspectoId.HasValue && dto.ProspectoId.Value > 0;

            if (tieneCliente == tieneProspecto)
                throw new ReglaNegocioException("Debe seleccionar un cliente o un prospecto como beneficiario, no ambos.");

            if (tieneCliente)
            {
                var cliente = await _context.Clientes.FindAsync(dto.ClienteId!.Value)
                    ?? throw new ReglaNegocioException("El cliente indicado no existe.");

                if (cliente.Estado != EstadoCliente.Activo)
                    throw new ReglaNegocioException("Solo se puede solicitar pro bono para un cliente activo.");
            }
            else
            {
                var prospecto = await _context.Prospectos.FindAsync(dto.ProspectoId!.Value)
                    ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

                if (prospecto.Estado != EstadoProspecto.Activo)
                    throw new ReglaNegocioException("Solo se puede solicitar pro bono para un prospecto activo.");
            }

            var solicitud = new SolicitudProBono
            {
                ClienteId = tieneCliente ? dto.ClienteId : null,
                ProspectoId = tieneProspecto ? dto.ProspectoId : null,
                SolicitanteId = usuarioActualId,
                JustificacionEscrita = dto.JustificacionEscrita,
                Decision = DecisionProBono.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            _context.SolicitudesProBono.Add(solicitud);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.InnerException?.Message ?? ex.Message);
            }

            // El id autogenerado de la solicitud solo existe después del primer SaveChanges
            // (Registrar() solo agrega al DbContext, así que este segundo SaveChanges es el
            // único punto donde de verdad se confirma en la base — no hay dos transacciones
            // de negocio distintas, solo una segunda escritura para la bitácora una vez que
            // ya existe el id real a referenciar).
            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "ProBono",
                registroAfectadoId: solicitud.SolicitudProBonoId.ToString(),
                valorNuevo: $"Solicitud pro bono presentada: {dto.JustificacionEscrita}");

            await _context.SaveChangesAsync();

            return solicitud.SolicitudProBonoId;
        }

        public async Task Resolver(int id, ResolverSolicitudProBonoDTO dto, string usuarioActualId)
        {
            var puedeResolver = await PuedeVerTodasAsync(usuarioActualId);
            if (!puedeResolver)
                throw new ReglaNegocioException("No tiene permiso para resolver solicitudes pro bono.");

            var solicitud = await _context.SolicitudesProBono
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.SolicitudProBonoId == id)
                ?? throw new ReglaNegocioException("La solicitud indicada no existe.");

            if (solicitud.Decision != DecisionProBono.Pendiente)
                throw new ReglaNegocioException("Esta solicitud ya fue resuelta.");

            if (dto.Decision is null || dto.Decision == DecisionProBono.Pendiente)
                throw new ReglaNegocioException("Debe seleccionar aprobar o rechazar la solicitud.");

            solicitud.Decision = dto.Decision.Value;
            solicitud.ComentarioResolucion = dto.ComentarioResolucion;
            solicitud.ResueltoPorId = usuarioActualId;
            solicitud.FechaResolucion = DateTime.UtcNow;

            // RF-011: un servicio pro bono no genera factura por su monto real (queda en
            // cero) — el único mecanismo que ya implementa eso hoy es Cliente.ModalidadPago
            // == ProBono (ver ExpedienteService.CerrarExpediente / HU-021), que sigue
            // generando la fila de Factura para trazabilidad, solo que con MontoTotal = 0.
            // Se decidió con el usuario (2026-09-18) que aprobar esta solicitud actualice
            // automáticamente esa modalidad cuando el beneficiario ya es un Cliente, para
            // que la aprobación formal realmente garantice el efecto que pide el RF, sin
            // pasos manuales aparte. Si el beneficiario es un Prospecto, no hay Cliente
            // todavía sobre el cual aplicar esto — queda pendiente de que, al convertirlo,
            // quien lo haga elija manualmente "Pro Bono" como modalidad de pago.
            if (solicitud.Decision == DecisionProBono.Aprobada && solicitud.Cliente is not null)
                solicitud.Cliente.ModalidadPago = ModalidadPago.ProBono;

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "ProBono",
                registroAfectadoId: solicitud.SolicitudProBonoId.ToString(),
                valorAnterior: "pendiente",
                valorNuevo: $"{solicitud.Decision.ToString().ToLower()}: {dto.ComentarioResolucion}");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
