namespace HikariLegalSRL.Models.DTOs
{
    public class EvaluacionCalidadListaDTO
    {
        public int EvaluacionId { get; set; }
        public int ExpedienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
        public int Puntuacion { get; set; }
        public string? ComentarioResumen { get; set; }
        public bool ComentarioTieneMas { get; set; }
        public string EvaluadoPorNombre { get; set; } = null!;
        public DateTime FechaEvaluacion { get; set; }
    }
}
