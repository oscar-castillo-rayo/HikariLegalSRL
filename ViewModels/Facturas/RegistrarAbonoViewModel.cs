using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.ViewModels.Facturas
{
    public class RegistrarAbonoViewModel
    {
        public int FacturaId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal SaldoPendiente { get; set; }
        public EstadoFactura Estado { get; set; }

        public AbonoRegistroDTO Abono { get; set; } = new();
    }
}
