namespace HikariLegalSRL.Models.DTOs
{
    public class EvaluacionCalidadDetalleDTO
    {
        public int EvaluacionId { get; set; }
        public int ExpedienteId { get; set; }
        public int PropuestaId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int TotalTareas { get; set; }
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
        public string EvaluadoPorNombre { get; set; } = null!;
        public DateTime FechaEvaluacion { get; set; }
    }
}
