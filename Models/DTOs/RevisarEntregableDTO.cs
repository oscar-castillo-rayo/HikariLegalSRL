using Microsoft.AspNetCore.Http;

namespace HikariLegalSRL.Models.DTOs
{
    public class RevisarEntregableDTO
    {
        public string? Observaciones { get; set; }
        public IFormFile? Archivo { get; set; }
    }
}
