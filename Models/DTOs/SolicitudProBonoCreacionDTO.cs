using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class SolicitudProBonoCreacionDTO
    {
        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }

        [Display(Name = "Prospecto")]
        public int? ProspectoId { get; set; }

        [Required(ErrorMessage = "La justificación es requerida")]
        [MaxLength(2000, ErrorMessage = "La justificación no puede superar los 2000 caracteres")]
        [Display(Name = "Justificación")]
        public string JustificacionEscrita { get; set; } = null!;
    }
}
