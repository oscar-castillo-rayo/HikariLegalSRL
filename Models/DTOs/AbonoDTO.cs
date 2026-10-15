using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class AbonoDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public string? NumeroComprobante { get; set; }
        public bool TieneComprobante { get; set; }
        public string RegistradoPorNombre { get; set; } = null!;
    }
}
