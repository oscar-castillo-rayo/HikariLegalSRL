using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface INotificacionService
    {
        // Crea la notificación si no existe ya una igual (mismo tipo + misma entidad + mismo
        // destinatario) — evita duplicados cada vez que corre la barrida periódica.
        Task NotificarSiNoExiste(string usuarioId, TipoNotificacion tipo, string mensaje, EntidadNotificacion entidadTipo, int entidadId);

        // Crea la notificación siempre, sin deduplicar — para eventos puntuales (cambio de
        // estado, reasignación) donde cada ocurrencia es una notificación nueva y válida.
        Task Notificar(string usuarioId, TipoNotificacion tipo, string mensaje, EntidadNotificacion entidadTipo, int entidadId);

        Task<List<NotificacionDTO>> ObtenerParaUsuario(string usuarioId);

        Task<int> ContarNoLeidas(string usuarioId);

        Task<string> MarcarLeidaYObtenerUrl(int notificacionId, string usuarioId);

        Task Eliminar(int notificacionId, string usuarioId);
    }
}
