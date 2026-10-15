using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ClienteEdicionDTO
    {
        [Required(ErrorMessage = "El nombre de empresa o persona es requerido")]
        [Display(Name = "Nombre de la empresa o persona")]
        public string NombreEmpresaPersona { get; set; } = null!;

        [Display(Name = "Nombre de contacto")]
        public string? NombreContacto { get; set; }

        [Display(Name = "Cédula jurídica")]
        public string? CedulaJuridica { get; set; }

        [StringLength(20, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres")]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }

        [Display(Name = "Sector económico")]
        public string? SectorEconomico { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la modalidad de pago")]
        [Display(Name = "Modalidad de pago")]
        public ModalidadPago? ModalidadPago { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        public DireccionCreacionDTO Direccion { get; set; } = null!;
    }
}
