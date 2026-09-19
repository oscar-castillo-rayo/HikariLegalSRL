namespace HikariLegalSRL.Constants
{
   // Permisos define los códigos de permisos utilizados en la aplicación, organizados por módulos y acciones. 
   // Cada módulo contiene una lista de acciones que representan los permisos específicos que se pueden asignar a 
   // los roles dentro del sistema. 
    public static class Permisos
    {
        // Tipo de Claim con el que se almacena cada permiso en `AspNetRoleClaims`.
        public const string ClaimType = "Permiso";

        // Prefijo que identifica una política de permiso dinámica.
        public const string Prefijo = "Permiso:";

        public static class Roles
        {
            public const string Ver               = "roles.ver";
            public const string Crear             = "roles.crear";
            public const string Editar            = "roles.editar";
            public const string Activar           = "roles.activar";
            public const string GestionarPermisos = "roles.gestionar_permisos";
        }

        public static class Usuarios
        {
            public const string Ver            = "usuarios.ver";
            public const string Crear          = "usuarios.crear";
            public const string Editar         = "usuarios.editar";
            public const string Activar        = "usuarios.activar";
            public const string GestionarRoles = "usuarios.gestionar_roles";
        }

        public static class Prospectos
        {
            public const string Ver       = "prospectos.ver";
            public const string Crear     = "prospectos.crear";
            public const string Editar    = "prospectos.editar";
            public const string Calificar = "prospectos.calificar";
            public const string Convertir = "prospectos.convertir";
            public const string Descartar = "prospectos.descartar";
            public const string Reactivar = "prospectos.reactivar";
        }

        public static class Clientes
        {
            public const string Ver             = "clientes.ver";
            public const string Crear           = "clientes.crear";
            public const string Editar          = "clientes.editar";
            public const string Asignar         = "clientes.asignar";
            public const string Desactivar      = "clientes.desactivar";
            public const string Reactivar       = "clientes.reactivar";
            public const string SerResponsable  = "clientes.ser_responsable";
        }

        public static class Servicios
        {
            public const string Ver        = "servicios.ver";
            public const string Crear      = "servicios.crear";
            public const string Editar     = "servicios.editar";
            public const string Desactivar = "servicios.desactivar";
            public const string Reactivar  = "servicios.reactivar";
        }

        public static class Propuestas
        {
            public const string Ver     = "propuestas.ver";
            public const string Crear   = "propuestas.crear";
            public const string Editar  = "propuestas.editar";
            public const string Enviar   = "propuestas.enviar";
            public const string Aceptar  = "propuestas.aceptar";
            public const string Rechazar = "propuestas.rechazar";
        }

        public static class Expedientes
        {
            public const string Ver             = "expedientes.ver";
            public const string Crear           = "expedientes.crear";
            public const string Asignar         = "expedientes.asignar";
            public const string Reasignar       = "expedientes.reasignar";
            public const string Cerrar          = "expedientes.cerrar";
            public const string Aprobar         = "expedientes.aprobar";
            public const string Devolver        = "expedientes.devolver";
            public const string Cargar          = "expedientes.cargar";
            public const string SerResponsable  = "expedientes.ser_responsable";
            public const string GestionarAjenas = "expedientes.gestionar_ajenas";
            public const string Supervisar      = "expedientes.supervisar";
            public const string GestionarTareasPropias = "expedientes.gestionar_tareas_propias";
            public const string EditarTarea     = "expedientes.editar_tarea";
            public const string EliminarTarea   = "expedientes.eliminar_tarea";
        }

        public static class Facturacion
        {
            public const string Ver            = "facturacion.ver";
            public const string Generar        = "facturacion.generar";
            public const string Anular         = "facturacion.anular";
            public const string Abono          = "facturacion.abono";
            public const string Probono        = "facturacion.probono";
            public const string AprobarProbono = "facturacion.aprobar_probono";
        }

        public static class Reportes
        {
            public const string Conversion = "reportes.conversion";
            public const string Ingresos   = "reportes.ingresos";
            public const string Geo        = "reportes.geo";
            public const string Carga      = "reportes.carga";
            public const string Calidad    = "reportes.calidad";
            public const string Servicios  = "reportes.servicios";
        }

        public static class Calidad
        {
            public const string Ver       = "calidad.ver";
            public const string Registrar = "calidad.registrar";
        }

        public static class Auditoria
        {
            public const string Ver = "auditoria.ver";
        }

        // Todos los códigos declarados arriba. Usado por `PermisosCatalogo.Validar()`.
        public static readonly IReadOnlyList<string> Todos = new[]
        {
            Roles.Ver, Roles.Crear, Roles.Editar, Roles.Activar, Roles.GestionarPermisos,
            Usuarios.Ver, Usuarios.Crear, Usuarios.Editar, Usuarios.Activar, Usuarios.GestionarRoles,
            Prospectos.Ver, Prospectos.Crear, Prospectos.Editar, Prospectos.Calificar, Prospectos.Convertir, Prospectos.Descartar, Prospectos.Reactivar,
            Clientes.Ver, Clientes.Crear, Clientes.Editar, Clientes.Asignar, Clientes.Desactivar, Clientes.Reactivar, Clientes.SerResponsable,
            Servicios.Ver, Servicios.Crear, Servicios.Editar, Servicios.Desactivar, Servicios.Reactivar,
            Propuestas.Ver, Propuestas.Crear, Propuestas.Editar, Propuestas.Enviar, Propuestas.Aceptar, Propuestas.Rechazar,
            Expedientes.Ver, Expedientes.Crear, Expedientes.Asignar, Expedientes.Reasignar, Expedientes.Cerrar, Expedientes.Aprobar, Expedientes.Devolver, Expedientes.Cargar, Expedientes.SerResponsable, Expedientes.GestionarAjenas, Expedientes.Supervisar, Expedientes.GestionarTareasPropias, Expedientes.EditarTarea, Expedientes.EliminarTarea,
            Facturacion.Ver, Facturacion.Generar, Facturacion.Anular, Facturacion.Abono, Facturacion.Probono, Facturacion.AprobarProbono,
            Reportes.Conversion, Reportes.Ingresos, Reportes.Geo, Reportes.Carga, Reportes.Calidad, Reportes.Servicios,
            Calidad.Ver, Calidad.Registrar,
            Auditoria.Ver,
        };
    }
}
