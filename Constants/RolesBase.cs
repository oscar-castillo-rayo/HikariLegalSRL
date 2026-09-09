namespace HikariLegalSRL.Constants
{
    // Roles base del sistema (RF-001) y sus permisos por defecto. El seed (`DbInitializer`) crea estos
    // roles la primera vez; a partir de ahí el Administrador los ajusta desde la pantalla de Roles y el
    // seed ya no vuelve a tocar sus permisos (salvo los del rol fijo Administrador, que se sincronizan
    // siempre con el catálogo completo).
    public static class RolesBase
    {
        public const string Administrador      = "Administrador";
        public const string AbogadoAsesor      = "Abogado/Asesor";
        public const string ColaboradorExterno = "Colaborador externo";
        public const string Asistente          = "Asistente";

        public record Definicion(string Nombre, string Descripcion, bool EsFijo);

        public static readonly IReadOnlyList<Definicion> Todos = new List<Definicion>
        {
            new(Administrador,      "Rol de administrador con acceso total al sistema", EsFijo: true),
            new(AbogadoAsesor,      "Profesional interno con una cartera de clientes, expedientes y propuestas asignada", EsFijo: false),
            new(ColaboradorExterno, "Profesional externo que ejecuta tareas asignadas dentro de un expediente", EsFijo: false),
            new(Asistente,          "Personal de apoyo administrativo de la firma", EsFijo: false),
        };

        // Permisos por defecto por rol. Derivados de 88_IIsem2026_Proyecto_TemaTFG_OscarCastillo
        // (relación de involucrados y "valores por defecto" del control de acceso). Editables por el
        // Administrador después; el Administrador recibe el catálogo completo desde `DbInitializer`.
        public static readonly IReadOnlyDictionary<string, string[]> PermisosPorDefecto =
            new Dictionary<string, string[]>
            {
                [AbogadoAsesor] = new[]
                {
                    Permisos.Prospectos.Ver, Permisos.Prospectos.Crear, Permisos.Prospectos.Editar,
                    Permisos.Prospectos.Calificar, Permisos.Prospectos.Convertir,
                    Permisos.Clientes.Ver, Permisos.Clientes.Crear, Permisos.Clientes.Editar,
                    Permisos.Propuestas.Ver, Permisos.Propuestas.Crear, Permisos.Propuestas.Editar, Permisos.Propuestas.Enviar,
                    Permisos.Expedientes.Ver, Permisos.Expedientes.Crear, Permisos.Expedientes.Asignar,
                    Permisos.Expedientes.Aprobar, Permisos.Expedientes.Devolver, Permisos.Expedientes.Cargar,
                    Permisos.Facturacion.Ver, Permisos.Facturacion.Probono,
                },
                [ColaboradorExterno] = new[]
                {
                    Permisos.Expedientes.Ver, Permisos.Expedientes.Cargar,
                    Permisos.Facturacion.Probono,
                },
                [Asistente] = new[]
                {
                    Permisos.Prospectos.Ver, Permisos.Prospectos.Crear, Permisos.Prospectos.Editar,
                    Permisos.Prospectos.Calificar,
                    Permisos.Clientes.Ver, Permisos.Clientes.Editar,
                    Permisos.Facturacion.Ver, Permisos.Facturacion.Abono, Permisos.Facturacion.Probono,
                },
            };
    }
}
