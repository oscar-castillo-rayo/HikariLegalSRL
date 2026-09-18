using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace HikariLegalSRL.Models.DTOs
{
    public class AbonoRegistroDTO
    {
        [Required(ErrorMessage = "El monto del abono es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha del abono es requerida")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Debe seleccionar el método de pago")]
        public MetodoPago? MetodoPago { get; set; }

        [MaxLength(50)]
        public string? NumeroComprobante { get; set; }

        public IFormFile? Comprobante { get; set; }
    }
}
