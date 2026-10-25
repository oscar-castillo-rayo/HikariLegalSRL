using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class PropuestaCreacionDTO
    {
        public int? ProspectoId { get; set; }

        public int? ClienteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la moneda")]
        public Moneda? Moneda { get; set; }

        [Required(ErrorMessage = "El plazo de entrega es requerido")]
        [Range(1, 3650, ErrorMessage = "El plazo debe ser mayor a 0 días")]
        public int PlazoDias { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la modalidad de pago")]
        public ModalidadPago? ModalidadPago { get; set; }

        [MaxLength(2000, ErrorMessage = "La descripción general no puede superar los 2000 caracteres")]
        public string? DescripcionGeneral { get; set; }

        public List<PropuestaServicioItemDTO> Servicios { get; set; } = new();
    }
}
