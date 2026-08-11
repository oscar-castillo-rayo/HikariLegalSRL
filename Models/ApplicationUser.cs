using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string NombreCompleto { get; set; }
        public string Especialidad { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
