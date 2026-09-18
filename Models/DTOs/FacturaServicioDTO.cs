namespace HikariLegalSRL.Models.DTOs
{
    public class FacturaServicioDTO
    {
        public string NombreServicio { get; set; } = null!;
        public string? DescripcionServicio { get; set; }
        public decimal Precio { get; set; }
    }
}
