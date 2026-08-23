namespace HikariLegalSRL.ViewModels.Roles
{

    // ViewModel para representar una acción de permiso, incluyendo su código, nombre, descripción y si está asignada al rol.
    public class PermisoAccionViewModel
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Asignado { get; set; }
    }
}
