namespace HikariLegalSRL.Models.DTOs
{
    public class TareaProximaDTO
    {
        public int Id { get; set; }
        public int ExpedienteId { get; set; }
        public string Descripcion { get; set; } = null!;
        public DateTime FechaLimite { get; set; }
    }
}
