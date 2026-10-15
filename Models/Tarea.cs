using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Tarea
    {
        public int TareaId { get; set; }

        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public string Descripcion { get; set; } = null!;

        public string ColaboradorResponsableId { get; set; } = null!;
        public ApplicationUser ColaboradorResponsable { get; set; } = null!;

        public DateTime FechaLimite { get; set; }
        public decimal HorasEstimadas { get; set; }
        public PrioridadTarea Prioridad { get; set; }
        public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;

        public DateTime FechaCreacion { get; set; }
    }
}
