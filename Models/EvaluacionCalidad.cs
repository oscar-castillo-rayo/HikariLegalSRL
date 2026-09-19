namespace HikariLegalSRL.Models
{
    public class EvaluacionCalidad
    {
        public int EvaluacionId { get; set; }

        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public byte Puntuacion { get; set; }
        public string? Comentario { get; set; }

        public string EvaluadoPorId { get; set; } = null!;
        public ApplicationUser EvaluadoPor { get; set; } = null!;

        public DateTime FechaEvaluacion { get; set; }
    }
}
