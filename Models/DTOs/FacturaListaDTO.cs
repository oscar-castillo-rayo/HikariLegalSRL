using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class FacturaListaDTO
    {
        public int Id { get; set; }
        public int ExpedienteId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public ModalidadPago ModalidadPago { get; set; }
        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente => MontoTotal - MontoPagado;
        public EstadoFactura Estado { get; set; }
        public DateTime FechaEmision { get; set; }
    }
}
