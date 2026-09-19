using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class EvaluacionCalidadCreacionDTO
    {
        [Required(ErrorMessage = "Debe seleccionar una puntuación del 1 al 5")]
        [Range(1, 5, ErrorMessage = "La puntuación debe estar entre 1 y 5")]
        [Display(Name = "Puntuación")]
        public byte? Puntuacion { get; set; }

        [MaxLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres")]
        [Display(Name = "Comentario")]
        public string? Comentario { get; set; }
    }
}
