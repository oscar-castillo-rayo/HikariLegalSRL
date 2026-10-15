using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class IngresosPorMonedaDTO
    {
        public Moneda Moneda { get; set; }
        public decimal TotalFacturado { get; set; }
        public List<IngresoPorAreaDTO> Desglose { get; set; } = new();
    }
}
