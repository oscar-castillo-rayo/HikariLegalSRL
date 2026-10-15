using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificacionService _notificacionService;

        public DashboardService(ApplicationDbContext context, INotificacionService notificacionService)
        {
            _context = context;
            _notificacionService = notificacionService;
        }

        public async Task<DashboardDTO> ObtenerResumen(string usuarioId)
        {
            var limite = DateTime.Today.AddDays(3);

            var tareasProximas = await _context.Tareas
                .AsNoTracking()
                .Where(t => t.ColaboradorResponsableId == usuarioId
                    && t.Estado != EstadoTarea.Aprobada
                    && t.FechaLimite <= limite)
                .OrderBy(t => t.FechaLimite)
                .Select(t => new TareaProximaDTO
                {
                    Id = t.TareaId,
                    ExpedienteId = t.ExpedienteId,
                    Descripcion = t.Descripcion,
                    FechaLimite = t.FechaLimite
                })
                .ToListAsync();

            return new DashboardDTO
            {
                TareasProximas = tareasProximas,
                Notificaciones = await _notificacionService.ObtenerParaUsuario(usuarioId)
            };
        }
    }
}
