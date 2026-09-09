namespace HikariLegalSRL.Constants
{
    // Este código define un catálogo de permisos para la aplicación, organizados por módulos y acciones.
    public class PermisoAccion
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    // La clase `PermisoModulo` representa un módulo de permisos. Contiene el nombre y una lista de
    // acciones asociadas a ese módulo.
    public class PermisoModulo
    {
        public string Nombre { get; set; } = string.Empty;
        public List<PermisoAccion> Acciones { get; set; } = new();
    }

    // El catálogo de permisos se define como una clase estática que contiene una lista de módulos con sus acciones.
    public static class PermisosCatalogo
    {
        public static readonly List<PermisoModulo> Modulos = new()
        {
            new PermisoModulo
            {
                Nombre = "Roles",
                Acciones = new()
                {
                    new() { Codigo = "roles.ver",                Nombre = "Consultar",          Descripcion = "Ver listado de roles" },
                    new() { Codigo = "roles.crear",             Nombre = "Crear",              Descripcion = "Registrar nuevo rol" },
                    new() { Codigo = "roles.editar",            Nombre = "Editar",             Descripcion = "Modificar datos del rol" },
                    new() { Codigo = "roles.activar",           Nombre = "Activar/Desactivar", Descripcion = "Cambiar estado del rol" },
                    new() { Codigo = "roles.gestionar_permisos", Nombre = "Gestionar permisos", Descripcion = "Asignar y revocar permisos de un rol" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Usuarios",
                Acciones = new()
                {
                    new() { Codigo = "usuarios.ver",             Nombre = "Consultar",          Descripcion = "Ver listado de usuarios" },
                    new() { Codigo = "usuarios.crear",           Nombre = "Crear",              Descripcion = "Registrar nuevo usuario" },
                    new() { Codigo = "usuarios.editar",          Nombre = "Editar",             Descripcion = "Modificar datos y rol del usuario" },
                    new() { Codigo = "usuarios.activar",         Nombre = "Activar/Desactivar", Descripcion = "Cambiar estado del usuario" },
                    new() { Codigo = "usuarios.gestionar_roles", Nombre = "Gestionar roles",    Descripcion = "Crear y editar roles desde el módulo de usuarios" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Prospectos",
                Acciones = new()
                {
                    new() { Codigo = "prospectos.ver",       Nombre = "Consultar",     Descripcion = "Ver listado y ficha de prospectos" },
                    new() { Codigo = "prospectos.crear",     Nombre = "Crear",         Descripcion = "Registrar nuevo prospecto" },
                    new() { Codigo = "prospectos.editar",    Nombre = "Editar",        Descripcion = "Modificar datos y actividades de seguimiento" },
                    new() { Codigo = "prospectos.calificar", Nombre = "Calificar",     Descripcion = "Asignar puntuación de 1 a 5" },
                    new() { Codigo = "prospectos.convertir", Nombre = "Convertir",     Descripcion = "Convertir el prospecto en cliente activo" },
                    new() { Codigo = "prospectos.descartar", Nombre = "Descartar",     Descripcion = "Marcar el prospecto como descartado" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Clientes",
                Acciones = new()
                {
                    new() { Codigo = "clientes.ver",        Nombre = "Consultar",           Descripcion = "Ver listado y ficha de clientes" },
                    new() { Codigo = "clientes.crear",      Nombre = "Crear",               Descripcion = "Registrar perfil de cliente" },
                    new() { Codigo = "clientes.editar",     Nombre = "Editar",              Descripcion = "Actualizar la ficha del cliente" },
                    new() { Codigo = "clientes.asignar",    Nombre = "Asignar responsable", Descripcion = "Asignar o reasignar el abogado/asesor responsable" },
                    new() { Codigo = "clientes.desactivar", Nombre = "Desactivar",          Descripcion = "Eliminación lógica del cliente" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Propuestas",
                Acciones = new()
                {
                    new() { Codigo = "propuestas.ver",     Nombre = "Consultar",       Descripcion = "Ver listado y detalle de propuestas" },
                    new() { Codigo = "propuestas.crear",   Nombre = "Crear",           Descripcion = "Crear propuesta en estado borrador" },
                    new() { Codigo = "propuestas.editar",  Nombre = "Editar",          Descripcion = "Modificar una propuesta en borrador" },
                    new() { Codigo = "propuestas.enviar",  Nombre = "Enviar",          Descripcion = "Marcar la propuesta como enviada al cliente" },
                    new() { Codigo = "propuestas.aceptar", Nombre = "Aceptar/Rechazar", Descripcion = "Registrar la respuesta del cliente" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Expedientes",
                Acciones = new()
                {
                    new() { Codigo = "expedientes.ver",      Nombre = "Consultar",   Descripcion = "Ver expedientes y tareas" },
                    new() { Codigo = "expedientes.crear",    Nombre = "Crear tareas", Descripcion = "Crear tareas dentro del expediente" },
                    new() { Codigo = "expedientes.asignar",  Nombre = "Asignar tareas", Descripcion = "Asignar tareas a colaboradores" },
                    new() { Codigo = "expedientes.cerrar",   Nombre = "Cerrar",      Descripcion = "Cerrar el expediente" },
                    new() { Codigo = "expedientes.aprobar",  Nombre = "Aprobar entregable", Descripcion = "Aprobar un entregable presentado" },
                    new() { Codigo = "expedientes.devolver", Nombre = "Devolver entregable", Descripcion = "Devolver un entregable para corrección" },
                    new() { Codigo = "expedientes.cargar",   Nombre = "Cargar entregable", Descripcion = "Subir un entregable de una tarea" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Facturación",
                Acciones = new()
                {
                    new() { Codigo = "facturacion.ver",              Nombre = "Consultar",         Descripcion = "Ver facturas y estado de cuenta" },
                    new() { Codigo = "facturacion.generar",          Nombre = "Generar factura",   Descripcion = "Emitir una factura" },
                    new() { Codigo = "facturacion.anular",           Nombre = "Anular factura",    Descripcion = "Anular una factura no pagada por completo" },
                    new() { Codigo = "facturacion.abono",            Nombre = "Registrar abono",   Descripcion = "Registrar un pago contra una factura" },
                    new() { Codigo = "facturacion.probono",          Nombre = "Solicitar pro bono", Descripcion = "Presentar una solicitud pro bono" },
                    new() { Codigo = "facturacion.aprobar_probono",  Nombre = "Aprobar pro bono",  Descripcion = "Resolver una solicitud pro bono" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Reportes",
                Acciones = new()
                {
                    new() { Codigo = "reportes.conversion", Nombre = "Tasa de conversión",   Descripcion = "Conversión de prospectos y propuestas" },
                    new() { Codigo = "reportes.ingresos",   Nombre = "Ingresos por servicio", Descripcion = "Ingresos facturados por tipo de servicio" },
                    new() { Codigo = "reportes.geo",        Nombre = "Segmentación geográfica", Descripcion = "Clientes por país, provincia y cantón" },
                    new() { Codigo = "reportes.carga",      Nombre = "Carga por colaborador", Descripcion = "Carga de trabajo por colaborador" },
                    new() { Codigo = "reportes.calidad",    Nombre = "Evaluación de calidad", Descripcion = "Reporte de evaluaciones de calidad" },
                }
            },
            new PermisoModulo
            {
                Nombre = "Auditoría",
                Acciones = new()
                {
                    new() { Codigo = "auditoria.ver", Nombre = "Consultar bitácora", Descripcion = "Ver la bitácora de auditoría del sistema" },
                }
            },
        };

        // El método `Todas` devuelve todas las acciones de permisos disponibles en todos los módulos.
        public static IEnumerable<PermisoAccion> Todas() => Modulos.SelectMany(m => m.Acciones);

        // Códigos válidos del catálogo, para validación rápida.
        public static ISet<string> Codigos() => Todas().Select(a => a.Codigo).ToHashSet();

        // Guardia anti-deriva: verifica que el catálogo (datos) y las constantes `Permisos.*` (usadas en
        // los atributos `[Permiso(...)]`) describan exactamente el mismo conjunto de códigos. Se invoca
        // al arrancar en entorno de desarrollo para detectar typos u olvidos de inmediato.
        public static void Validar()
        {
            var catalogo = Codigos();
            var duplicados = Todas()
                .GroupBy(a => a.Codigo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicados.Count > 0)
                throw new InvalidOperationException(
                    $"PermisosCatalogo tiene códigos duplicados: {string.Join(", ", duplicados)}");

            var constantes = Permisos.Todos.ToHashSet();

            var faltanEnConstantes = catalogo.Except(constantes).ToList();
            var faltanEnCatalogo = constantes.Except(catalogo).ToList();

            if (faltanEnConstantes.Count > 0 || faltanEnCatalogo.Count > 0)
                throw new InvalidOperationException(
                    "PermisosCatalogo y Permisos.* están desalineados. " +
                    $"Sólo en el catálogo: [{string.Join(", ", faltanEnConstantes)}]. " +
                    $"Sólo en las constantes: [{string.Join(", ", faltanEnCatalogo)}].");
        }
    }
}
