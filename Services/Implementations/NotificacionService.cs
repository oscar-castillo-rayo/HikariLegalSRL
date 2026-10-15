using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class NotificacionService : INotificacionService
    {
        private readonly ApplicationDbContext _context;

        public NotificacionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task NotificarSiNoExiste(string usuarioId, TipoNotificacion tipo, string mensaje, EntidadNotificacion entidadTipo, int entidadId)
        {
            var yaExiste = await _context.Notificaciones.AnyAsync(n =>
                n.UsuarioId == usuarioId
                && n.Tipo == tipo
                && n.EntidadRelacionadaTipo == entidadTipo
                && n.EntidadRelacionadaId == entidadId);

            if (yaExiste)
                return;

            await Notificar(usuarioId, tipo, mensaje, entidadTipo, entidadId);
        }

        public async Task Notificar(string usuarioId, TipoNotificacion tipo, string mensaje, EntidadNotificacion entidadTipo, int entidadId)
        {
            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId,
                Tipo = tipo,
                Mensaje = mensaje,
                EntidadRelacionadaTipo = entidadTipo,
                EntidadRelacionadaId = entidadId,
                Leida = false,
                FechaCreacion = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificacionDTO>> ObtenerParaUsuario(string usuarioId)
        {
            var notificaciones = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.UsuarioId == usuarioId)
                .OrderBy(n => n.Leida)
                .ThenByDescending(n => n.FechaCreacion)
                .ToListAsync();

            var resultado = new List<NotificacionDTO>();
            foreach (var n in notificaciones)
            {
                resultado.Add(new NotificacionDTO
                {
                    Id = n.NotificacionId,
                    Tipo = n.Tipo,
                    Mensaje = n.Mensaje,
                    Leida = n.Leida,
                    FechaCreacion = n.FechaCreacion,
                    Url = await ResolverUrl(n.EntidadRelacionadaTipo, n.EntidadRelacionadaId)
                });
            }

            return resultado;
        }

        public async Task<int> ContarNoLeidas(string usuarioId)
        {
            return await _context.Notificaciones.CountAsync(n => n.UsuarioId == usuarioId && !n.Leida);
        }

        public async Task<string> MarcarLeidaYObtenerUrl(int notificacionId, string usuarioId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId)
                ?? throw new ReglaNegocioException("La notificación indicada no existe.");

            if (notificacion.UsuarioId != usuarioId)
                throw new ReglaNegocioException("Esta notificación no le pertenece.");

            notificacion.Leida = true;
            await _context.SaveChangesAsync();

            return await ResolverUrl(notificacion.EntidadRelacionadaTipo, notificacion.EntidadRelacionadaId);
        }

        public async Task Eliminar(int notificacionId, string usuarioId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId)
                ?? throw new ReglaNegocioException("La notificación indicada no existe.");

            if (notificacion.UsuarioId != usuarioId)
                throw new ReglaNegocioException("Esta notificación no le pertenece.");

            _context.Notificaciones.Remove(notificacion);
            await _context.SaveChangesAsync();
        }

        private async Task<string> ResolverUrl(EntidadNotificacion entidadTipo, int entidadId)
        {
            switch (entidadTipo)
            {
                case EntidadNotificacion.Expediente:
                    return $"/Expedientes/Detalle/{entidadId}";

                case EntidadNotificacion.Tarea:
                    var expedienteId = await _context.Tareas
                        .Where(t => t.TareaId == entidadId)
                        .Select(t => (int?)t.ExpedienteId)
                        .FirstOrDefaultAsync();
                    return expedienteId is null ? "/Expedientes/Index" : $"/Expedientes/Detalle/{expedienteId}";

                case EntidadNotificacion.ActividadSeguimiento:
                    var prospectoId = await _context.ActividadesSeguimiento
                        .Where(a => a.ActividadSeguimientoId == entidadId)
                        .Select(a => (int?)a.ProspectoId)
                        .FirstOrDefaultAsync();
                    return prospectoId is null ? "/Prospectos/Index" : $"/Prospectos/Detalle/{prospectoId}";

                default:
                    return "/Dashboard/Index";
            }
        }
    }
}
