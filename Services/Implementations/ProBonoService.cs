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
    /// <summary>
    /// Implementa IProBonoService: contiene la lógica de las solicitudes pro bono y su resolución.
    /// </summary>
    public class ProBonoService : IProBonoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;
        private readonly ITransaccionService _transaccionService;

        public ProBonoService(
            ApplicationDbContext context,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService,
            ITransaccionService transaccionService)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
            _transaccionService = transaccionService;
        }

        // Quien puede aprobar/rechazar ve todo el historial; el resto, solo sus solicitudes.
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
                    FechaResolucion = s.FechaResolucion,
                    PropuestaConsumidaId = s.PropuestaConsumidaId
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
                FechaResolucion = solicitud.FechaResolucion,
                PropuestaConsumidaId = solicitud.PropuestaConsumidaId
            };
        }

        public async Task<int> Crear(SolicitudProBonoCreacionDTO dto, string usuarioActualId)
            => await _transaccionService.EjecutarAsync(() => CrearInterno(dto, usuarioActualId));

        private async Task<int> CrearInterno(SolicitudProBonoCreacionDTO dto, string usuarioActualId)
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

            // La bitácora se registra después del primer SaveChanges porque necesita el id generado.
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
            => await _transaccionService.EjecutarAsync(() => ResolverInterno(id, dto, usuarioActualId));

        private async Task ResolverInterno(int id, ResolverSolicitudProBonoDTO dto, string usuarioActualId)
        {
            var puedeResolver = await PuedeVerTodasAsync(usuarioActualId);
            if (!puedeResolver)
                throw new ReglaNegocioException("No tiene permiso para resolver solicitudes pro bono.");

            var solicitud = await _context.SolicitudesProBono
                .FirstOrDefaultAsync(s => s.SolicitudProBonoId == id)
                ?? throw new ReglaNegocioException("La solicitud indicada no existe.");

            if (solicitud.Decision != DecisionProBono.Pendiente)
                throw new ReglaNegocioException("Esta solicitud ya fue resuelta.");

            if (dto.Decision is null || dto.Decision == DecisionProBono.Pendiente)
                throw new ReglaNegocioException("Debe seleccionar aprobar o rechazar la solicitud.");

            if (string.IsNullOrWhiteSpace(dto.ComentarioResolucion))
                throw new ReglaNegocioException("Debe indicar la justificación de la decisión.");

            solicitud.Decision = dto.Decision.Value;
            solicitud.ComentarioResolucion = dto.ComentarioResolucion.Trim();
            solicitud.ResueltoPorId = usuarioActualId;
            solicitud.FechaResolucion = DateTime.UtcNow;

            // Aprobar solo deja la solicitud disponible para respaldar una propuesta Pro Bono
            // (ver PropuestaService.ValidarYReservarSolicitudProBono).

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "ProBono",
                registroAfectadoId: solicitud.SolicitudProBonoId.ToString(),
                valorAnterior: "pendiente",
                valorNuevo: $"{solicitud.Decision.ToString().ToLower()}: {solicitud.ComentarioResolucion}");

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
