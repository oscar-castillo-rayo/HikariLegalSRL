using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ClienteEdicionDTO
    {
        [Required(ErrorMessage = "El nombre de empresa o persona es requerido")]
        public string NombreEmpresaPersona { get; set; } = null!;

        public string? NombreContacto { get; set; }

        public string? CedulaJuridica { get; set; }

        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        public string Correo { get; set; } = null!;

        public string? SectorEconomico { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la modalidad de pago")]
        public ModalidadPago? ModalidadPago { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        public DireccionCreacionDTO Direccion { get; set; } = null!;
    }
}
