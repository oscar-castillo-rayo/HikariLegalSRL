using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class FacturaDetalleDTO
    {
        public int Id { get; set; }
        public int ExpedienteId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
        public ModalidadPago ModalidadPago { get; set; }
        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public EstadoFactura Estado { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public List<FacturaServicioDTO> Servicios { get; set; } = new();

        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public List<AbonoDTO> Abonos { get; set; } = new();
    }
}
