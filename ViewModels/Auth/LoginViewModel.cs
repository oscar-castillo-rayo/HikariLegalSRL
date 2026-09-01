using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HikariLegalSRL.ViewModels.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [DisplayName("Correo Electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [DisplayName("Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        public bool RecordarSesion { get; set; }
    }
}
