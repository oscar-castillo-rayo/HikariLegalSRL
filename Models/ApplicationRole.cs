using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string? Descripcion { get; set; }
        public bool EsFijo { get; set; }
    }
}
