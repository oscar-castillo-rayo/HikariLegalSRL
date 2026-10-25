using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.ViewModels.Usuarios
{
    public class CrearUsuarioViewModel
    {
        [Display(Name = "Nombre completo")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string NombreCompleto { get; set; }

        [Display(Name = "Correo electrónico")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        public string Correo { get; set; }

        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Contrasena { get; set; }

        [Display(Name = "Confirmar contraseña")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public string RolId { get; set; }

        [Display(Name = "Especialidad")]
        [StringLength(100, ErrorMessage = "La especialidad no puede superar los 100 caracteres.")]
        public string? Especialidad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public IEnumerable<SelectListItem> RolesDisponibles { get; set; } = new List<SelectListItem>();
    }
}