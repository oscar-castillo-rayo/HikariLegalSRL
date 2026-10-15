using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Notificacion
    {
        public int NotificacionId { get; set; }

        public string UsuarioId { get; set; } = null!;
        public ApplicationUser Usuario { get; set; } = null!;

        public TipoNotificacion Tipo { get; set; }
        public string Mensaje { get; set; } = null!;

        public EntidadNotificacion EntidadRelacionadaTipo { get; set; }
        public int EntidadRelacionadaId { get; set; }

        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
