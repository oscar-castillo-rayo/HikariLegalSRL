using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ActividadSeguimientoFormDTO
    {
        [Required(ErrorMessage = "El tipo de actividad es requerido")]
        public TipoActividad Tipo { get; set; }

        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres")]
        public string Titulo { get; set; } = null!;

        [Required(ErrorMessage = "La fecha y hora de la actividad son requeridas")]
        public DateTime FechaHora { get; set; }

        [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El responsable es requerido")]
        public string ResponsableId { get; set; } = null!;
    }
}
