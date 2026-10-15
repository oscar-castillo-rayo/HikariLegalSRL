using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HikariLegalSRL.Models.DTOs
{
    public class CargarEntregableDTO
    {
        [Range(0, 99, ErrorMessage = "Ingrese un tiempo válido")]
        public int? Horas { get; set; }

        [Range(0, 59, ErrorMessage = "Ingrese un tiempo válido")]
        public int? Minutos { get; set; }

        public IFormFile? Archivo { get; set; }
    }
}
