using HikariLegalSRL.Extensions;

namespace HikariLegalSRL.Models.DTOs
{
    public class DashboardDTO
    {
        public List<TareaProximaDTO> TareasProximas { get; set; } = new();
        public List<NotificacionDTO> Notificaciones { get; set; } = new();

        public int TareasVencidas => TareasProximas.Count(t => t.FechaLimite.Date < HoraCostaRica.Hoy);
        public int TareasPorVencer => TareasProximas.Count(t => t.FechaLimite.Date >= HoraCostaRica.Hoy);
        public int NotificacionesNoLeidas => Notificaciones.Count(n => !n.Leida);
    }
}
