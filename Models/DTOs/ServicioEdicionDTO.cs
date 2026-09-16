using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ServicioEdicionDTO
    {
        [Required(ErrorMessage = "El nombre del servicio es requerido")]
        [MaxLength(150)]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El área o categoría es requerida")]
        [MaxLength(100)]
        public string AreaCategoria { get; set; } = null!;

        [MaxLength(1000)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio base es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio base no puede ser negativo")]
        public decimal PrecioBase { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el tipo de servicio")]
        public TipoServicio? TipoServicio { get; set; }
    }
}
