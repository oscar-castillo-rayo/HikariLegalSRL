namespace HikariLegalSRL.ViewModels.Roles
{

    // ViewModel para gestionar los permisos de los roles.
    public class GestionarPermisosViewModel
    {
        public List<RolListaViewModel> Roles { get; set; } = new();
        public RolDetalleViewModel? RolSeleccionado { get; set; }

    }
}
