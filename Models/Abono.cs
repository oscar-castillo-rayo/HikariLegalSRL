using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Abono
    {
        public int AbonoId { get; set; }

        public int FacturaId { get; set; }
        public Factura Factura { get; set; } = null!;

        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string? NumeroComprobante { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public string? ComprobanteArchivo { get; set; }

        public string RegistradoPorId { get; set; } = null!;
        public ApplicationUser RegistradoPor { get; set; } = null!;
    }
}
