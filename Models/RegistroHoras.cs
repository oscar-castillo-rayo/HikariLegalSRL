using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class RegistroHoras
    {
        public int RegistroHorasId { get; set; }

        public int TareaId { get; set; }
        public Tarea Tarea { get; set; } = null!;

        public int RondaRevision { get; set; }

        public string UsuarioId { get; set; } = null!;
        public ApplicationUser Usuario { get; set; } = null!;

        public RolHoras Rol { get; set; }
        public int Minutos { get; set; }

        public DateTime FechaHora { get; set; }
    }
}
