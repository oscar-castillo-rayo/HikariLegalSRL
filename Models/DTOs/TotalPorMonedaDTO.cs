using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class TotalPorMonedaDTO
    {
        public Moneda Moneda { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal TotalPendiente => TotalFacturado - TotalPagado;
    }
}
