namespace HikariLegalSRL.Models.DTOs
{
    public class ExpedienteEvaluableDTO
    {
        public int ExpedienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
        public DateTime? FechaCierre { get; set; }
    }
}
