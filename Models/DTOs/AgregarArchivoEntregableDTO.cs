using Microsoft.AspNetCore.Http;

namespace HikariLegalSRL.Models.DTOs
{
    public class AgregarArchivoEntregableDTO
    {
        public List<IFormFile> Archivos { get; set; } = new();
    }
}
