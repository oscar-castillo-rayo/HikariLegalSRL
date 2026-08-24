namespace HikariLegalSRL.ViewModels.Roles
{
    // ViewModel para representar un módulo de permisos, incluyendo su nombre y las acciones asociadas.
    public class PermisoModuloViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public List<PermisoAccionViewModel> Acciones { get; set; } = new();
    }
}
