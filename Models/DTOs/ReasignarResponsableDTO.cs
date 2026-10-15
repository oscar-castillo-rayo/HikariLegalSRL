using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class ReasignarResponsableDTO
    {
        [Required(ErrorMessage = "Debe seleccionar un responsable")]
        public string? ResponsableId { get; set; }
    }
}
