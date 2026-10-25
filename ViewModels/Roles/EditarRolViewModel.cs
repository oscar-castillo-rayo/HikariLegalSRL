using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.ViewModels.Roles
{
    public class EditarRolViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string? Descripcion { get; set; }
        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }
}
