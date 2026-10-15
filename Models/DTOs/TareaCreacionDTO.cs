using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class TareaCreacionDTO
    {
        [Required(ErrorMessage = "La descripción es requerida")]
        [MaxLength(1000)]
        public string Descripcion { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar un colaborador responsable")]
        public string? ColaboradorResponsableId { get; set; }

        [Required(ErrorMessage = "La fecha límite es requerida")]
        [DataType(DataType.Date)]
        public DateTime? FechaLimite { get; set; }

        [Required(ErrorMessage = "Las horas estimadas son requeridas")]
        [Range(0.01, 999, ErrorMessage = "Las horas estimadas deben ser mayores a 0")]
        public decimal? HorasEstimadas { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una prioridad")]
        public PrioridadTarea? Prioridad { get; set; }
    }
}
