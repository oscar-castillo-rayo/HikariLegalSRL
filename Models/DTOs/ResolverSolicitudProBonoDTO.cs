using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ResolverSolicitudProBonoDTO
    {
        [Required(ErrorMessage = "Debe seleccionar aprobar o rechazar")]
        [Display(Name = "Decisión")]
        public DecisionProBono? Decision { get; set; }

        [Required(ErrorMessage = "Debe indicar la justificación de la decisión")]
        [MaxLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres")]
        [Display(Name = "Comentario de resolución")]
        public string ComentarioResolucion { get; set; } = null!;
    }
}
