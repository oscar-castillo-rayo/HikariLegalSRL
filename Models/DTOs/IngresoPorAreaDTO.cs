namespace HikariLegalSRL.Models.DTOs
{
    public class IngresoPorAreaDTO
    {
        public string AreaCategoria { get; set; } = null!;
        public decimal MontoFacturado { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
