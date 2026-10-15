using Microsoft.AspNetCore.Http;

namespace HikariLegalSRL.Models.DTOs
{
    public class CargarEntregableDTO
    {
        public IFormFile? Archivo { get; set; }
    }
}
