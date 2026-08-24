namespace HikariLegalSRL.Constants
{
    namespace HikariLegalSRL.Constants
    {
        // Este código define un catálogo de permisos para la aplicación, organizados por módulos y acciones.
        // Cada módulo contiene una lista de acciones que representan los permisos específicos que se pueden asignar a los usuarios o roles dentro del sistema.
        public class PermisoAccion
        {
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
        }

        // La clase `PermisoModulo` representa un módulo de permisos, que contiene un nombre y una lista de acciones asociadas a ese módulo.
        public class PermisoModulo
        {
            public string Nombre { get; set; } = string.Empty;
            public List<PermisoAccion> Acciones { get; set; } = new();
        }

        // El catálogo de permisos se define como una clase estática que contiene una lista de módulos, cada uno con sus respectivas acciones.
        public static class PermisosCatalogo
        {
            public static readonly List<PermisoModulo> Modulos = new()
        {
            new PermisoModulo
            {
                Nombre = "Usuarios",
                Acciones = new()
                {
                    new() { Codigo = "us_ver",     Nombre = "Consultar",          Descripcion = "Ver listado de usuarios" },
                    new() { Codigo = "us_crear",   Nombre = "Crear",              Descripcion = "Registrar nuevo usuario" },
                    new() { Codigo = "us_editar",  Nombre = "Editar",             Descripcion = "Modificar datos del usuario" },
                    new() { Codigo = "us_activar", Nombre = "Activar/Desactivar", Descripcion = "Cambiar estado del usuario" },
                    new() { Codigo = "us_gestionar_roles",   Nombre = "Gestionar roles",    Descripcion = "Crear, editar y asignar permisos a roles" }
                }
            },
            new PermisoModulo
            {
                Nombre = "Roles",
                Acciones = new()
                {
                    new() { Codigo = "rol_ver",     Nombre = "Consultar",          Descripcion = "Ver listado de roles" },
                    new() { Codigo = "rol_crear",   Nombre = "Crear",              Descripcion = "Registrar nuevo rol" },
                    new() { Codigo = "rol_editar",  Nombre = "Editar",             Descripcion = "Modificar datos del rol" },
                    new() { Codigo = "rol_activar", Nombre = "Activar/Desactivar", Descripcion = "Cambiar estado del rol" },
                    new() { Codigo = "rol_gestionar_permisos",Nombre = "Gestionar permisos", Descripcion = "Asignar permisos a roles" }
                }
            },
        };

            // El método `Todas` devuelve una lista de todas las acciones de permisos disponibles en todos los módulos, lo que permite obtener un conjunto completo de permisos para la aplicación.
            public static IEnumerable<PermisoAccion> Todas() => Modulos.SelectMany(m => m.Acciones);
        }
    }
}
