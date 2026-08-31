using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class ProspectoCreacionDTO
    {
        [Required(ErrorMessage = "El nombre de empresa o persona es requerido")]
        public string NombreEmpresaPersona { get; set; } = null!;
        
        [Required(ErrorMessage = "El nombre de contacto es requerido")]
        public string NombreContacto { get; set; } = null!;
        
        public string? CedulaJuridica { get; set; }
        
        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
        public string Telefono { get; set; } = null!;
        
        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        public string Correo { get; set; } = null!;
        
        public string? Sector { get; set; }
        
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public byte? Calificacion { get; set; }
        
        public string? Observaciones { get; set; }
        
        [Required(ErrorMessage = "La dirección es requerida")]
        public DireccionCreacionDTO Direccion { get; set; } = null!;
    }
}