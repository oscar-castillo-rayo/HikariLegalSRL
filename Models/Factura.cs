using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Factura
    {
        public int FacturaId { get; set; }

        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public ModalidadPago ModalidadPago { get; set; }
        public decimal MontoTotal { get; set; }
        public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;

        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAnulacion { get; set; }
    }
}
