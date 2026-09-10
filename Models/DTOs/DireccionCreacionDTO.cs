using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{   
    public class DireccionCreacionDTO
    {
        [Required(ErrorMessage = "Debe seleccionar un país")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un país válido")]
        public int? PaisId { get; set; }
        
        public int? DistritoId { get; set; }
        
        public string? SenasExactas { get; set; }
    }
}