namespace HikariLegalSRL.ViewModels.Usuarios
{
    public class UsuarioListaViewModel
    {
        public string Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public string? Especialidad { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
        public DateTime Creado { get; set; }
    }
}
