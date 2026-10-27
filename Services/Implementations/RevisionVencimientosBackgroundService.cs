using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Extensions;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    /// <summary>
    /// Revisa periódicamente tareas, expedientes y actividades de seguimiento
    /// próximos a vencer o ya vencidos, y genera las notificaciones correspondientes.
    /// NotificarSiNoExiste evita duplicar la misma alerta en cada barrida.
    /// </summary>
    public class RevisionVencimientosBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RevisionVencimientosBackgroundService> _logger;

        public RevisionVencimientosBackgroundService(IServiceScopeFactory scopeFactory, ILogger<RevisionVencimientosBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RevisarVencimientosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revisando vencimientos para notificaciones.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }

        private async Task RevisarVencimientosAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();
            var permisoEvaluador = scope.ServiceProvider.GetRequiredService<IPermisoEvaluador>();

            var hoy = HoraCostaRica.Hoy;

            var supervisores = await permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Supervisar);

            var tareas = await context.Tareas
                .Include(t => t.Expediente)
                .Where(t => t.Estado != EstadoTarea.Aprobada && t.Expediente.Estado == EstadoExpediente.Abierto)
                .ToListAsync(stoppingToken);

            foreach (var tarea in tareas)
            {
                var diasRestantes = (tarea.FechaLimite.Date - hoy).Days;

                // Tres avisos: a 3 días al colaborador y al responsable; a 1 día se suman los
                // supervisores; vencida, los tres reciben un único aviso.
                if (diasRestantes < 0)
                {
                    var destinatariosVencida = new HashSet<string>(supervisores.Select(s => s.Id))
                    {
                        tarea.ColaboradorResponsableId,
                        tarea.Expediente.ResponsableId
                    };

                    foreach (var destinatarioId in destinatariosVencida)
                    {
                        await notificacionService.NotificarSiNoExiste(
                            destinatarioId, TipoNotificacion.AlertaTareaVencida,
                            $"La tarea '{tarea.Descripcion}' (expediente #{tarea.ExpedienteId}) venció el {tarea.FechaLimite:dd/MM/yyyy}.",
                            EntidadNotificacion.Tarea, tarea.TareaId);
                    }
                }
                else if (diasRestantes <= 3)
                {
                    var destinatariosProxima = new HashSet<string> { tarea.ColaboradorResponsableId, tarea.Expediente.ResponsableId };

                    foreach (var destinatarioId in destinatariosProxima)
                    {
                        await notificacionService.NotificarSiNoExiste(
                            destinatarioId, TipoNotificacion.AlertaTareaProxima,
                            $"La tarea '{tarea.Descripcion}' (expediente #{tarea.ExpedienteId}) vence el {tarea.FechaLimite:dd/MM/yyyy}.",
                            EntidadNotificacion.Tarea, tarea.TareaId);
                    }
                }

                if (diasRestantes is >= 0 and <= 1)
                {
                    var destinatariosEscalamiento = new HashSet<string>(supervisores.Select(s => s.Id))
                    {
                        tarea.ColaboradorResponsableId,
                        tarea.Expediente.ResponsableId
                    };

                    foreach (var destinatarioId in destinatariosEscalamiento)
                    {
                        await notificacionService.NotificarSiNoExiste(
                            destinatarioId, TipoNotificacion.EscalamientoAdminTarea,
                            $"La tarea '{tarea.Descripcion}' (expediente #{tarea.ExpedienteId}) vence el {tarea.FechaLimite:dd/MM/yyyy} y sigue sin completarse.",
                            EntidadNotificacion.Tarea, tarea.TareaId);
                    }
                }
            }

            var expedientes = await context.Expedientes
                .Where(e => e.Estado == EstadoExpediente.Abierto)
                .ToListAsync(stoppingToken);

            foreach (var expediente in expedientes)
            {
                var diasRestantes = (expediente.PlazoComprometido.Date - hoy).Days;

                // Avisos a 5 días, a 1 día y al vencer; el expediente vencido no se cierra solo.
                if (diasRestantes < 0)
                {
                    var destinatariosVencido = new HashSet<string>(supervisores.Select(s => s.Id))
                    {
                        expediente.ResponsableId
                    };

                    foreach (var destinatarioId in destinatariosVencido)
                    {
                        await notificacionService.NotificarSiNoExiste(
                            destinatarioId, TipoNotificacion.AlertaExpedienteVencido,
                            $"El expediente #{expediente.ExpedienteId} venció el {expediente.PlazoComprometido:dd/MM/yyyy}.",
                            EntidadNotificacion.Expediente, expediente.ExpedienteId);
                    }
                }
                else if (diasRestantes <= 5)
                {
                    await notificacionService.NotificarSiNoExiste(
                        expediente.ResponsableId, TipoNotificacion.AlertaExpedienteProximo,
                        $"El expediente #{expediente.ExpedienteId} vence el {expediente.PlazoComprometido:dd/MM/yyyy}.",
                        EntidadNotificacion.Expediente, expediente.ExpedienteId);
                }

                if (diasRestantes is >= 0 and <= 1)
                {
                    var destinatariosEscalamiento = new HashSet<string>(supervisores.Select(s => s.Id))
                    {
                        expediente.ResponsableId
                    };

                    foreach (var destinatarioId in destinatariosEscalamiento)
                    {
                        await notificacionService.NotificarSiNoExiste(
                            destinatarioId, TipoNotificacion.EscalamientoAdminExpediente,
                            $"El expediente #{expediente.ExpedienteId} vence el {expediente.PlazoComprometido:dd/MM/yyyy}.",
                            EntidadNotificacion.Expediente, expediente.ExpedienteId);
                    }
                }
            }

            var limiteSeguimiento = DateTime.UtcNow.AddHours(24);
            var actividades = await context.ActividadesSeguimiento
                .Where(a => a.FechaHora >= DateTime.UtcNow && a.FechaHora <= limiteSeguimiento)
                .ToListAsync(stoppingToken);

            foreach (var actividad in actividades)
            {
                await notificacionService.NotificarSiNoExiste(
                    actividad.ResponsableId, TipoNotificacion.AlertaSeguimientoProximo,
                    $"La actividad de seguimiento '{actividad.Titulo}' está programada para el {actividad.FechaHora.ALocal():dd/MM/yyyy HH:mm}.",
                    EntidadNotificacion.ActividadSeguimiento, actividad.ActividadSeguimientoId);
            }
        }
    }
}
