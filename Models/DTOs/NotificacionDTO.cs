using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class NotificacionDTO
    {
        public int Id { get; set; }
        public TipoNotificacion Tipo { get; set; }
        public string Mensaje { get; set; } = null!;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Url { get; set; } = null!;
    }
}
