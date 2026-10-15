using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.Models.DTOs
{
    public class ProspectoCreacionDTO
    {
        [Required(ErrorMessage = "El nombre de empresa o persona es requerido")]
        [Display(Name = "Nombre de la empresa o persona")]
        public string NombreEmpresaPersona { get; set; } = null!;

        [Required(ErrorMessage = "El nombre de contacto es requerido")]
        [Display(Name = "Nombre de contacto")]
        public string NombreContacto { get; set; } = null!;

        [Display(Name = "Cédula jurídica")]
        public string? CedulaJuridica { get; set; }

        [StringLength(20, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres")]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }

        public string? Sector { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        [Display(Name = "Calificación")]
        public byte? Calificacion { get; set; }
        
        public string? Observaciones { get; set; }
        
        [Required(ErrorMessage = "La dirección es requerida")]
        public DireccionCreacionDTO Direccion { get; set; } = null!;
    }
}