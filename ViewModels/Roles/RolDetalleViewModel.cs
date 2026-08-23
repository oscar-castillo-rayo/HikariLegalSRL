namespace HikariLegalSRL.ViewModels.Roles
{
    // ViewModel para mostrar los detalles de un rol específico, incluyendo sus permisos.
    public class RolDetalleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public bool EsFijo { get; set; }
        public List<PermisoModuloViewModel> Modulos { get; set; } = new();
    }
}
