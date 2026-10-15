using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class PropuestaServicioItemDTO
    {
        [Required(ErrorMessage = "Debe seleccionar un servicio")]
        public int? ServicioId { get; set; }

        [MaxLength(1000)]
        public string? DescripcionServicio { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }
    }
}
