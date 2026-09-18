using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class TareaEdicionDTO
    {
        [Required(ErrorMessage = "La descripción es requerida")]
        [MaxLength(1000)]
        public string Descripcion { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar un colaborador responsable")]
        public string? ColaboradorResponsableId { get; set; }

        [Required(ErrorMessage = "La fecha límite es requerida")]
        [DataType(DataType.Date)]
        public DateTime? FechaLimite { get; set; }

        [Range(0, 99, ErrorMessage = "Ingrese un tiempo válido")]
        public int? HorasEstimadasHoras { get; set; }

        [Range(0, 59, ErrorMessage = "Ingrese un tiempo válido")]
        public int? HorasEstimadasMinutos { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una prioridad")]
        public PrioridadTarea? Prioridad { get; set; }
    }
}
