using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class AgregarHorasDTO
    {
        [Range(0, 99, ErrorMessage = "Ingrese un tiempo válido")]
        public int? Horas { get; set; }

        [Range(0, 59, ErrorMessage = "Ingrese un tiempo válido")]
        public int? Minutos { get; set; }
    }
}
