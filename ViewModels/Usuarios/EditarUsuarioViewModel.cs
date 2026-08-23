using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.ViewModels.Usuarios
{
    public class EditarUsuarioViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string NombreCompleto { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        public string Correo { get; set; }

        [StringLength(100)]
        public string? Especialidad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public string RolId { get; set; }

        public bool Activo { get; set; }

        public IEnumerable<SelectListItem> RolesDisponibles { get; set; } = new List<SelectListItem>();

    }
}
