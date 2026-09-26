using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Expedientes;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ExpedienteService : IExpedienteService
    {
        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".zip", ".txt"
        };
        private const long TamanoMaximoArchivoBytes = 20 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;
        private readonly INotificacionService _notificacionService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ITransaccionService _transaccionService;
        private readonly ILogger<ExpedienteService> _logger;

        public ExpedienteService(
            ApplicationDbContext context,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService,
            INotificacionService notificacionService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ITransaccionService transaccionService,
            ILogger<ExpedienteService> logger)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
            _notificacionService = notificacionService;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _transaccionService = transaccionService;
            _logger = logger;
        }

        // RF-009: notifica cambio de estado de una tarea al colaborador asignado, al
        // responsable del expediente (cartera) y a quien tenga permiso de supervisión
        // (equivalente a "Administrador" pero por permiso, no por rol) — sin duplicar al
        // propio usuario que ejecutó la acción.
        private async Task NotificarCambioEstadoTareaAsync(Tarea tarea, string usuarioActualId, string mensaje)
        {
            var destinatarios = new HashSet<string> { tarea.ColaboradorResponsableId, tarea.Expediente.ResponsableId };

            var supervisores = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Supervisar);
            foreach (var supervisor in supervisores)
                destinatarios.Add(supervisor.Id);

            destinatarios.Remove(usuarioActualId);

            foreach (var destinatarioId in destinatarios)
            {
                await _notificacionService.Notificar(
                    destinatarioId, TipoNotificacion.CambioEstadoTarea, mensaje,
                    EntidadNotificacion.Tarea, tarea.TareaId);
            }
        }

        private string ObtenerCarpetaEntregables()
        {
            var rutaConfigurada = _configuration["Almacenamiento:EntregablesPath"] ?? "App_Data/entregables";
            return Path.Combine(_webHostEnvironment.ContentRootPath, rutaConfigurada);
        }

        private static decimal CombinarHorasMinutos(int? horas, int? minutos)
            => Math.Round((horas ?? 0) + (minutos ?? 0) / 60m, 2);

        // Ronda que corresponde al trabajo actual. Mientras está en revisión, es la ronda del
        // entregable que se está evaluando. En cualquier otro estado, si el último entregable
        // ya tiene una revisión (fue aprobado o devuelto) esa ronda quedó cerrada y ahora se
        // trabaja la siguiente; si el último entregable todavía NO tiene revisión, es el
        // borrador de la ronda actual (creado al primer archivo adjuntado con
        // AgregarArchivoEntregable, antes incluso de enviar la tarea a revisión), así que
        // sigue siendo esa misma ronda — no una nueva — mientras se le sigan agregando archivos.
        private static int RondaActual(Tarea tarea)
        {
            if (tarea.Entregables.Count == 0)
                return 1;

            var ultimo = tarea.Entregables.OrderByDescending(e => e.RondaRevision).First();

            if (tarea.Estado == EstadoTarea.ListaRevision)
                return ultimo.RondaRevision;

            return ultimo.Revisiones.Any() ? ultimo.RondaRevision + 1 : ultimo.RondaRevision;
        }

        private async Task<bool> PuedeGestionarTareaAjenaAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Expedientes.GestionarAjenas);
        }

        private async Task<bool> PuedeGestionarTareasPropiasAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Expedientes.GestionarTareasPropias);
        }

        public async Task<List<ExpedienteListaDTO>> Listar(string usuarioActualId)
        {
            var puedeVerTodos = await PuedeGestionarTareaAjenaAsync(usuarioActualId);

            var query = _context.Expedientes
                .AsNoTracking()
                .Include(e => e.Cliente)
                .Include(e => e.Responsable)
                .Include(e => e.Tareas)
                .AsQueryable();

            // RF-007: el Administrador ve todos los expedientes; el Abogado/Asesor solo
            // los de su cartera (donde es responsable); el Colaborador externo solo los
            // que tienen alguna tarea asignada a él. No depende del rol, solo de los datos.
            if (!puedeVerTodos)
            {
                query = query.Where(e => e.ResponsableId == usuarioActualId
                    || e.Tareas.Any(t => t.ColaboradorResponsableId == usuarioActualId));
            }

            return await query
                .OrderByDescending(e => e.FechaApertura)
                .Select(e => new ExpedienteListaDTO
                {
                    Id = e.ExpedienteId,
                    ClienteNombre = e.Cliente.NombreEmpresaPersona,
                    ResponsableNombre = e.Responsable.NombreCompleto,
                    PlazoComprometido = e.PlazoComprometido,
                    Estado = e.Estado,
                    TotalTareas = e.Tareas.Count,
                    TareasAprobadas = e.Tareas.Count(t => t.Estado == EstadoTarea.Aprobada),
                    Evaluado = _context.EvaluacionesCalidad.Any(ev => ev.ExpedienteId == e.ExpedienteId)
                })
                .ToListAsync();
        }

        public async Task<ExpedienteDetalleViewModel?> ObtenerDetalle(int id, string usuarioActualId)
        {
            var expediente = await _context.Expedientes
                .AsNoTracking()
                .Include(e => e.Cliente)
                .Include(e => e.Responsable)
                .Include(e => e.Tareas).ThenInclude(t => t.ColaboradorResponsable)
                .Include(e => e.Tareas).ThenInclude(t => t.Entregables).ThenInclude(en => en.Archivos)
                .Include(e => e.Tareas).ThenInclude(t => t.Entregables).ThenInclude(en => en.Revisiones).ThenInclude(r => r.Revisor)
                .Include(e => e.Tareas).ThenInclude(t => t.RegistrosHoras)
                .FirstOrDefaultAsync(e => e.ExpedienteId == id);

            if (expediente is null)
                return null;

            // RF-007: mismo criterio de cartera que Listar().
            var puedeVerTodos = await PuedeGestionarTareaAjenaAsync(usuarioActualId);
            var tieneAcceso = puedeVerTodos
                || expediente.ResponsableId == usuarioActualId
                || expediente.Tareas.Any(t => t.ColaboradorResponsableId == usuarioActualId);

            if (!tieneAcceso)
                return null;

            var facturaId = expediente.Estado == EstadoExpediente.Cerrado
                ? await _context.Facturas
                    .Where(f => f.ExpedienteId == id)
                    .Select(f => (int?)f.FacturaId)
                    .FirstOrDefaultAsync()
                : null;

            var evaluado = expediente.Estado == EstadoExpediente.Cerrado
                && await _context.EvaluacionesCalidad.AnyAsync(ev => ev.ExpedienteId == id);

            return new ExpedienteDetalleViewModel
            {
                Expediente = new ExpedienteDetalleDTO
                {
                    Id = expediente.ExpedienteId,
                    ClienteNombre = expediente.Cliente.NombreEmpresaPersona,
                    PropuestaId = expediente.PropuestaId,
                    ResponsableId = expediente.ResponsableId,
                    ResponsableNombre = expediente.Responsable.NombreCompleto,
                    PlazoComprometido = expediente.PlazoComprometido,
                    FechaApertura = expediente.FechaApertura,
                    FechaCierre = expediente.FechaCierre,
                    Estado = expediente.Estado,
                    FacturaId = facturaId,
                    Evaluado = evaluado,
                    Tareas = expediente.Tareas
                        .OrderBy(t => t.FechaLimite)
                        .Select(t => new TareaListaDTO
                        {
                            Id = t.TareaId,
                            Descripcion = t.Descripcion,
                            ColaboradorId = t.ColaboradorResponsableId,
                            ColaboradorNombre = t.ColaboradorResponsable.NombreCompleto,
                            FechaLimite = t.FechaLimite,
                            HorasEstimadas = t.HorasEstimadas,
                            Prioridad = t.Prioridad,
                            Estado = t.Estado,
                            FechaCreacion = t.FechaCreacion,
                            ArchivosRondaActual = t.Entregables
                                .Where(en => en.RondaRevision == RondaActual(t))
                                .SelectMany(en => en.Archivos)
                                .Select(a => new ArchivoDTO { Id = a.EntregableArchivoId, NombreOriginal = a.NombreOriginal })
                                .ToList(),
                            HorasColaborador = Math.Round((t.RegistrosHoras
                                .Where(r => r.Rol == RolHoras.Colaborador)
                                .Sum(r => (int?)r.Minutos) ?? 0) / 60m, 2),
                            HorasRevisor = Math.Round((t.RegistrosHoras
                                .Where(r => r.Rol == RolHoras.Revisor)
                                .Sum(r => (int?)r.Minutos) ?? 0) / 60m, 2),
                            HorasRondaActual = Math.Round((t.RegistrosHoras
                                .Where(r => r.Rol == RolHoras.Colaborador && r.RondaRevision == RondaActual(t))
                                .Sum(r => (int?)r.Minutos) ?? 0) / 60m, 2),
                            HorasRevisionRondaActual = Math.Round((t.RegistrosHoras
                                .Where(r => r.Rol == RolHoras.Revisor && r.RondaRevision == RondaActual(t))
                                .Sum(r => (int?)r.Minutos) ?? 0) / 60m, 2),
                            Historial = t.Entregables
                                .OrderByDescending(en => en.RondaRevision)
                                .Select(en => new RondaHistorialDTO
                                {
                                    RondaRevision = en.RondaRevision,
                                    EntregableId = en.EntregableId,
                                    Archivos = en.Archivos
                                        .Select(a => new ArchivoDTO { Id = a.EntregableArchivoId, NombreOriginal = a.NombreOriginal })
                                        .ToList(),
                                    HorasReales = en.HorasReales,
                                    FechaCarga = en.FechaCarga,
                                    RevisionId = en.Revisiones
                                        .Select(r => (int?)r.RevisionId)
                                        .FirstOrDefault(),
                                    Resultado = en.Revisiones
                                        .Select(r => (ResultadoRevision?)r.Resultado)
                                        .FirstOrDefault(),
                                    RevisorNombre = en.Revisiones
                                        .Select(r => r.Revisor.NombreCompleto)
                                        .FirstOrDefault(),
                                    HorasRevision = en.Revisiones
                                        .Select(r => (decimal?)r.HorasRevision)
                                        .FirstOrDefault(),
                                    Observaciones = en.Revisiones
                                        .Select(r => r.Observaciones)
                                        .FirstOrDefault(),
                                    TieneArchivoRevision = en.Revisiones
                                        .Select(r => r.ArchivoAdjunto)
                                        .FirstOrDefault() != null,
                                    FechaRevision = en.Revisiones
                                        .Select(r => (DateTime?)r.FechaRevision)
                                        .FirstOrDefault()
                                }).ToList()
                        }).ToList()
                },
                Colaboradores = await ObtenerColaboradoresActivos(),
                Responsables = await ObtenerResponsablesActivos()
            };
        }

        public async Task<List<UsuarioOpcionDTO>> ObtenerResponsablesActivos()
        {
            var responsables = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.SerResponsable);
            return responsables
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new UsuarioOpcionDTO { Id = u.Id, Nombre = u.NombreCompleto })
                .ToList();
        }

        public async Task<List<UsuarioOpcionDTO>> ObtenerColaboradoresActivos()
        {
            var colaboradores = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Cargar);
            return colaboradores
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new UsuarioOpcionDTO { Id = u.Id, Nombre = u.NombreCompleto })
                .ToList();
        }

        public async Task AgregarTarea(int expedienteId, TareaCreacionDTO dto, string usuarioActualId)
        {
            var expediente = await _context.Expedientes
                .FirstOrDefaultAsync(e => e.ExpedienteId == expedienteId)
                ?? throw new ReglaNegocioException("El expediente indicado no existe.");

            if (expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden agregar tareas a expedientes abiertos.");

            var colaborador = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Cargar);
            if (!colaborador.Any(u => u.Id == dto.ColaboradorResponsableId))
                throw new ReglaNegocioException("El colaborador seleccionado no existe o no está disponible para recibir tareas.");

            var horasEstimadas = CombinarHorasMinutos(dto.HorasEstimadasHoras, dto.HorasEstimadasMinutos);
            if (horasEstimadas <= 0)
                throw new ReglaNegocioException("Las horas estimadas deben ser mayores a 0.");

            var tarea = new Tarea
            {
                ExpedienteId = expedienteId,
                Descripcion = dto.Descripcion,
                ColaboradorResponsableId = dto.ColaboradorResponsableId!,
                FechaLimite = dto.FechaLimite!.Value,
                HorasEstimadas = horasEstimadas,
                Prioridad = dto.Prioridad!.Value,
                Estado = EstadoTarea.Pendiente,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Tareas.Add(tarea);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Tareas",
                registroAfectadoId: tarea.TareaId.ToString(),
                valorNuevo: $"Tarea '{tarea.Descripcion}' asignada, expediente #{expedienteId}");

            await _context.SaveChangesAsync();
        }

        public async Task EditarTarea(int tareaId, TareaEdicionDTO dto, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden editar tareas de expedientes abiertos.");

            // RF-008: el historial de revisión es inalterable una vez aprobado el entregable
            // final; antes de eso (misma regla que para eliminar) sí se puede seguir editando.
            if (tarea.Estado == EstadoTarea.Aprobada)
                throw new ReglaNegocioException("No se puede editar una tarea ya aprobada.");

            var colaborador = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Cargar);
            if (!colaborador.Any(u => u.Id == dto.ColaboradorResponsableId))
                throw new ReglaNegocioException("El colaborador seleccionado no existe o no está disponible para recibir tareas.");

            var horasEstimadas = CombinarHorasMinutos(dto.HorasEstimadasHoras, dto.HorasEstimadasMinutos);
            if (horasEstimadas <= 0)
                throw new ReglaNegocioException("Las horas estimadas deben ser mayores a 0.");

            var descripcionAnterior = tarea.Descripcion;
            var colaboradorAnteriorId = tarea.ColaboradorResponsableId;

            tarea.Descripcion = dto.Descripcion;
            tarea.ColaboradorResponsableId = dto.ColaboradorResponsableId!;
            tarea.FechaLimite = dto.FechaLimite!.Value;
            tarea.HorasEstimadas = horasEstimadas;
            tarea.Prioridad = dto.Prioridad!.Value;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "editar",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: $"Tarea '{descripcionAnterior}'",
                valorNuevo: $"Tarea '{tarea.Descripcion}'");

            await _context.SaveChangesAsync();

            // RF-007 (equivalente a nivel de tarea): notificar al nuevo colaborador cuando
            // "Editar tarea" cambia a quién está asignada. El colaborador anterior no se
            // notifica: al dejar de ser ColaboradorResponsableId simplemente deja de ver la
            // tarea, no hace falta avisarle.
            if (colaboradorAnteriorId != tarea.ColaboradorResponsableId)
            {
                await _notificacionService.Notificar(
                    tarea.ColaboradorResponsableId, TipoNotificacion.ReasignacionTarea,
                    $"Se te asignó la tarea '{tarea.Descripcion}' (expediente #{tarea.ExpedienteId}).",
                    EntidadNotificacion.Tarea, tarea.TareaId);
            }
        }

        public async Task EliminarTarea(int tareaId, string usuarioActualId)
            => await _transaccionService.EjecutarAsync(() => EliminarTareaInterno(tareaId, usuarioActualId));

        private async Task EliminarTareaInterno(int tareaId, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .Include(t => t.Entregables).ThenInclude(en => en.Archivos)
                .Include(t => t.Entregables).ThenInclude(en => en.Revisiones)
                .Include(t => t.RegistrosHoras)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden eliminar tareas de expedientes abiertos.");

            // RF-008: el historial de revisión es inalterable una vez aprobado el entregable
            // final; antes de eso (incluso ya iniciada, con horas o entregables registrados)
            // sí se puede eliminar por completo, para corregir una tarea creada por error.
            if (tarea.Estado == EstadoTarea.Aprobada)
                throw new ReglaNegocioException("No se puede eliminar una tarea ya aprobada.");

            var descripcion = tarea.Descripcion;
            var estadoAnterior = tarea.Estado.ToString();
            var horasRegistradas = Math.Round(tarea.RegistrosHoras.Sum(r => r.Minutos) / 60m, 2);
            var totalEntregables = tarea.Entregables.Count;

            // Sin cascada automática en la base (FK en NoAction): hay que borrar primero las
            // filas dependientes antes de la tarea misma. Los archivos físicos se borran solo
            // cuando la transacción se confirma.
            var rutasArchivos = new List<string>();
            foreach (var entregable in tarea.Entregables)
            {
                foreach (var archivo in entregable.Archivos)
                    rutasArchivos.Add(Path.Combine(ObtenerCarpetaEntregables(), archivo.ArchivoRuta.Replace('/', Path.DirectorySeparatorChar)));

                foreach (var revision in entregable.Revisiones.Where(r => r.ArchivoAdjunto is not null))
                    rutasArchivos.Add(Path.Combine(ObtenerCarpetaEntregables(), revision.ArchivoAdjunto!.Replace('/', Path.DirectorySeparatorChar)));

                _context.RevisionesEntregable.RemoveRange(entregable.Revisiones);
                _context.EntregableArchivos.RemoveRange(entregable.Archivos);
            }

            _context.Entregables.RemoveRange(tarea.Entregables);
            _context.RegistrosHoras.RemoveRange(tarea.RegistrosHoras);
            _context.Tareas.Remove(tarea);

            await _context.SaveChangesAsync();

            _transaccionService.AlConfirmar(() =>
            {
                foreach (var ruta in rutasArchivos.Where(File.Exists))
                    File.Delete(ruta);
            });

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "eliminar",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: $"Tarea '{descripcion}' (estado: {estadoAnterior}, {horasRegistradas}h registradas, {totalEntregables} entregable(s))");
        }

        public async Task ReasignarResponsable(int expedienteId, string? nuevoResponsableId, string usuarioActualId)
        {
            var expediente = await _context.Expedientes
                .Include(e => e.Responsable)
                .FirstOrDefaultAsync(e => e.ExpedienteId == expedienteId)
                ?? throw new ReglaNegocioException("El expediente indicado no existe.");

            if (expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se puede reasignar el responsable de un expediente abierto.");

            if (string.IsNullOrWhiteSpace(nuevoResponsableId))
                throw new ReglaNegocioException("Debe seleccionar un responsable.");

            if (expediente.ResponsableId == nuevoResponsableId)
                throw new ReglaNegocioException("El expediente ya tiene asignado a ese responsable.");

            var disponibles = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.SerResponsable);
            var nuevoResponsable = disponibles.FirstOrDefault(u => u.Id == nuevoResponsableId)
                ?? throw new ReglaNegocioException("El responsable seleccionado no existe o no está disponible para asumir expedientes.");

            var responsableAnterior = expediente.Responsable.NombreCompleto;

            expediente.ResponsableId = nuevoResponsable.Id;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "editar",
                moduloAfectado: "Expedientes",
                registroAfectadoId: expedienteId.ToString(),
                valorAnterior: $"Responsable: {responsableAnterior}",
                valorNuevo: $"Responsable: {nuevoResponsable.NombreCompleto}");

            await _context.SaveChangesAsync();

            // RF-007: notificar al nuevo responsable asignado.
            await _notificacionService.Notificar(
                nuevoResponsable.Id, TipoNotificacion.ReasignacionExpediente,
                $"Ahora eres responsable del expediente #{expedienteId}.",
                EntidadNotificacion.Expediente, expedienteId);
        }

        public async Task CerrarExpediente(int expedienteId, string usuarioActualId)
            => await _transaccionService.EjecutarAsync(() => CerrarExpedienteInterno(expedienteId, usuarioActualId));

        private async Task CerrarExpedienteInterno(int expedienteId, string usuarioActualId)
        {
            var expediente = await _context.Expedientes
                .Include(e => e.Propuesta)
                .Include(e => e.Tareas)
                .FirstOrDefaultAsync(e => e.ExpedienteId == expedienteId)
                ?? throw new ReglaNegocioException("El expediente indicado no existe.");

            if (expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("El expediente ya está cerrado.");

            // RF-007 (criterio confirmado con el usuario, 2026-09-18: la lectura literal del
            // RF-007 solo menciona "pendiente"/"en proceso", pero se decidió exigir que todas
            // las tareas estén aprobadas, así que "devuelta" y "lista_revision" también bloquean).
            // También se exige al menos una tarea: sin esto, un expediente recién abierto (sin
            // tareas todavía) se podía cerrar de inmediato y facturar el monto completo sin
            // ningún trabajo registrado.
            if (expediente.Tareas.Count == 0 || expediente.Tareas.Any(t => t.Estado != EstadoTarea.Aprobada))
                throw new ReglaNegocioException("No se puede cerrar el expediente: debe tener al menos una tarea, y todas deben estar en estado 'Aprobada'.");

            expediente.Estado = EstadoExpediente.Cerrado;
            expediente.FechaCierre = DateTime.UtcNow;

            // RF-007/RF-011 (rediseñado 2026-09-18): si "pro bono" se define a nivel de Cliente,
            // cualquier expediente futuro de ese mismo cliente —sin relación con el caso pro bono
            // original— también facturaría en cero para siempre. Por eso el monto cero depende de
            // la Propuesta que originó este expediente específico (ya validada contra una
            // solicitud pro bono aprobada al crearla, ver PropuestaService), no del cliente.
            //
            // La modalidad de la factura también se toma de la Propuesta, no del Cliente: usar
            // Cliente.ModalidadPago (la lectura literal de RF-007) generaba facturas con monto
            // real pero etiquetadas "Pro bono" cuando el cliente había quedado con esa modalidad
            // por un caso anterior sin relación — contradictorio y confirmado en pruebas. La
            // modalidad que realmente aplicó a este expediente es la que se negoció y aceptó en
            // su propia propuesta.
            var esProBono = expediente.Propuesta.ModalidadPago == ModalidadPago.ProBono;

            var factura = new Factura
            {
                ExpedienteId = expediente.ExpedienteId,
                ClienteId = expediente.ClienteId,
                ModalidadPago = expediente.Propuesta.ModalidadPago,
                MontoTotal = esProBono ? 0 : expediente.Propuesta.MontoTotal,
                Estado = EstadoFactura.Emitida,
                FechaEmision = DateTime.UtcNow
            };
            _context.Facturas.Add(factura);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Expedientes",
                registroAfectadoId: expedienteId.ToString(),
                valorAnterior: "abierto",
                valorNuevo: "cerrado");

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Facturación",
                registroAfectadoId: factura.FacturaId.ToString(),
                valorNuevo: $"Factura generada al cerrar el expediente #{expedienteId}: modalidad {factura.ModalidadPago}, monto {factura.MontoTotal:N2}");

            await _context.SaveChangesAsync();
        }

        public async Task IniciarTarea(int tareaId, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden iniciar tareas de expedientes abiertos.");

            if (tarea.Estado is not (EstadoTarea.Pendiente or EstadoTarea.Devuelta))
                throw new ReglaNegocioException("Solo se puede iniciar una tarea que esté pendiente o devuelta.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede iniciar esta tarea.");

            var estadoAnterior = tarea.Estado == EstadoTarea.Pendiente ? "pendiente" : "devuelta";
            tarea.Estado = EstadoTarea.EnProceso;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: estadoAnterior,
                valorNuevo: "en_proceso");

            await _context.SaveChangesAsync();

            await NotificarCambioEstadoTareaAsync(tarea, usuarioActualId,
                $"La tarea '{tarea.Descripcion}' pasó a 'en proceso'.");
        }

        public async Task MarcarListaParaRevision(int tareaId, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .Include(t => t.Entregables).ThenInclude(en => en.Revisiones)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden actualizar tareas de expedientes abiertos.");

            if (tarea.Estado != EstadoTarea.EnProceso)
                throw new ReglaNegocioException("Solo se puede enviar a revisión una tarea que esté en proceso.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede completar esta tarea.");

            var rondaActual = RondaActual(tarea);
            var minutosRonda = await _context.RegistrosHoras
                .Where(r => r.TareaId == tareaId && r.RondaRevision == rondaActual && r.Rol == RolHoras.Colaborador)
                .SumAsync(r => (int?)r.Minutos) ?? 0;

            if (minutosRonda <= 0)
                throw new ReglaNegocioException("Debe registrar tiempo trabajado en esta ronda (con el botón + de horas) antes de enviar a revisión.");

            var horasReales = Math.Round(minutosRonda / 60m, 2);

            // Los archivos ya se van adjuntando de a uno con AgregarArchivoEntregable mientras
            // la tarea está en proceso; aquí solo se cierra la ronda con las horas reales.
            var entregable = tarea.Entregables.FirstOrDefault(en => en.RondaRevision == rondaActual);
            if (entregable is null)
            {
                entregable = new Entregable
                {
                    TareaId = tareaId,
                    RondaRevision = rondaActual,
                    HorasReales = horasReales,
                    TipoEntregable = TipoEntregable.Preliminar,
                    CargadoPorId = usuarioActualId,
                    FechaCarga = DateTime.UtcNow
                };
                _context.Entregables.Add(entregable);
            }
            else
            {
                entregable.HorasReales = horasReales;
            }

            tarea.Estado = EstadoTarea.ListaRevision;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: "en_proceso",
                valorNuevo: $"lista_revision (horas reales ronda {rondaActual}: {horasReales}h)");

            await _context.SaveChangesAsync();

            await NotificarCambioEstadoTareaAsync(tarea, usuarioActualId,
                $"La tarea '{tarea.Descripcion}' está lista para revisión.");
        }

        private async Task<Entregable> ObtenerOCrearEntregableRondaActualAsync(Tarea tarea, string usuarioActualId)
        {
            var rondaActual = RondaActual(tarea);
            var entregable = await _context.Entregables
                .FirstOrDefaultAsync(en => en.TareaId == tarea.TareaId && en.RondaRevision == rondaActual);

            if (entregable is null)
            {
                entregable = new Entregable
                {
                    TareaId = tarea.TareaId,
                    RondaRevision = rondaActual,
                    HorasReales = 0,
                    TipoEntregable = TipoEntregable.Preliminar,
                    CargadoPorId = usuarioActualId,
                    FechaCarga = DateTime.UtcNow
                };
                _context.Entregables.Add(entregable);
                await _context.SaveChangesAsync();
            }

            return entregable;
        }

        public async Task AgregarArchivoEntregable(int tareaId, AgregarArchivoEntregableDTO dto, string usuarioActualId)
            => await _transaccionService.EjecutarAsync(() => AgregarArchivoEntregableInterno(tareaId, dto, usuarioActualId));

        private async Task AgregarArchivoEntregableInterno(int tareaId, AgregarArchivoEntregableDTO dto, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .Include(t => t.Entregables).ThenInclude(en => en.Revisiones)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden adjuntar archivos en expedientes abiertos.");

            if (tarea.Estado is not (EstadoTarea.EnProceso or EstadoTarea.Devuelta))
                throw new ReglaNegocioException("Solo se pueden adjuntar archivos mientras la tarea está en proceso.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede adjuntar archivos a esta tarea.");

            var archivos = dto.Archivos.Where(a => a.Length > 0).ToList();
            if (archivos.Count == 0)
                throw new ReglaNegocioException("Debe seleccionar al menos un archivo.");

            foreach (var archivo in archivos)
            {
                if (archivo.Length > TamanoMaximoArchivoBytes)
                    throw new ReglaNegocioException($"El archivo '{archivo.FileName}' no puede superar los 20 MB.");

                var extensionArchivo = Path.GetExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(extensionArchivo) || !ExtensionesPermitidas.Contains(extensionArchivo))
                    throw new ReglaNegocioException($"El tipo de archivo de '{archivo.FileName}' no está permitido.");
            }

            var entregable = await ObtenerOCrearEntregableRondaActualAsync(tarea, usuarioActualId);

            var carpetaEntregable = Path.Combine(ObtenerCarpetaEntregables(), tareaId.ToString(), entregable.EntregableId.ToString());
            Directory.CreateDirectory(carpetaEntregable);

            var nombresArchivos = new List<string>();
            foreach (var archivo in archivos)
            {
                var nombreArchivo = Path.GetFileName(archivo.FileName);
                var nombreFisico = $"{Guid.NewGuid()}_{nombreArchivo}";
                var rutaFisica = Path.Combine(carpetaEntregable, nombreFisico);
                _transaccionService.AlRevertir(() =>
                {
                    if (File.Exists(rutaFisica))
                        File.Delete(rutaFisica);
                });
                using (var destino = File.Create(rutaFisica))
                {
                    await archivo.CopyToAsync(destino);
                }

                _context.EntregableArchivos.Add(new EntregableArchivo
                {
                    EntregableId = entregable.EntregableId,
                    ArchivoRuta = $"{tareaId}/{entregable.EntregableId}/{nombreFisico}",
                    NombreOriginal = nombreArchivo,
                    CargadoPorId = usuarioActualId,
                    FechaCarga = DateTime.UtcNow
                });

                nombresArchivos.Add(nombreArchivo);
            }

            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorNuevo: $"Archivo(s) adjuntado(s) en la ronda {entregable.RondaRevision}: {string.Join(", ", nombresArchivos)}");
        }

        public async Task EliminarArchivoEntregable(int archivoId, string usuarioActualId)
            => await _transaccionService.EjecutarAsync(() => EliminarArchivoEntregableInterno(archivoId, usuarioActualId));

        private async Task EliminarArchivoEntregableInterno(int archivoId, string usuarioActualId)
        {
            var archivo = await _context.EntregableArchivos
                .Include(a => a.Entregable).ThenInclude(en => en.Tarea).ThenInclude(t => t.Expediente)
                .FirstOrDefaultAsync(a => a.EntregableArchivoId == archivoId)
                ?? throw new ReglaNegocioException("El archivo indicado no existe.");

            var tarea = archivo.Entregable.Tarea;

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden eliminar archivos de expedientes abiertos.");

            if (tarea.Estado is not (EstadoTarea.EnProceso or EstadoTarea.Devuelta))
                throw new ReglaNegocioException("Solo se pueden eliminar archivos mientras la tarea está en proceso.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede eliminar archivos de esta tarea.");

            var rutaFisica = Path.Combine(ObtenerCarpetaEntregables(), archivo.ArchivoRuta.Replace('/', Path.DirectorySeparatorChar));
            var nombreOriginal = archivo.NombreOriginal;

            _context.EntregableArchivos.Remove(archivo);
            await _context.SaveChangesAsync();

            _transaccionService.AlConfirmar(() =>
            {
                if (File.Exists(rutaFisica))
                    File.Delete(rutaFisica);
            });

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "eliminar",
                moduloAfectado: "Tareas",
                registroAfectadoId: tarea.TareaId.ToString(),
                valorAnterior: nombreOriginal);
        }

        public async Task AgregarHoras(int tareaId, AgregarHorasDTO dto, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .Include(t => t.Entregables).ThenInclude(en => en.Revisiones)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden registrar horas en expedientes abiertos.");

            if (tarea.Estado is not (EstadoTarea.EnProceso or EstadoTarea.ListaRevision or EstadoTarea.Devuelta))
                throw new ReglaNegocioException("Solo se pueden registrar horas en una tarea activa.");

            // El rol depende del momento del flujo en que se registra, no de quién lo registra:
            // mientras se trabaja la tarea (en proceso o recién devuelta) cuenta como colaborador;
            // mientras está en revisión cuenta como revisor. Por eso quién puede registrar
            // también depende del estado: en proceso/devuelta, solo el colaborador asignado;
            // en revisión, solo quien puede revisar el expediente (el responsable de la
            // cartera) — si no, un Abogado/Asesor nunca podría registrar sus horas de revisión
            // sobre tareas de otros colaboradores en su propia cartera.
            var rol = tarea.Estado == EstadoTarea.ListaRevision ? RolHoras.Revisor : RolHoras.Colaborador;

            var puedeRegistrar = rol == RolHoras.Revisor
                ? tarea.Expediente.ResponsableId == usuarioActualId || await PuedeGestionarTareaAjenaAsync(usuarioActualId)
                : tarea.ColaboradorResponsableId == usuarioActualId || await PuedeGestionarTareaAjenaAsync(usuarioActualId);

            if (!puedeRegistrar)
                throw new ReglaNegocioException("No tiene permiso para registrar horas en esta tarea.");

            var minutos = (dto.Horas ?? 0) * 60 + (dto.Minutos ?? 0);
            if (minutos <= 0)
                throw new ReglaNegocioException("Ingrese un tiempo válido.");

            var registro = new RegistroHoras
            {
                TareaId = tareaId,
                RondaRevision = RondaActual(tarea),
                UsuarioId = usuarioActualId,
                Rol = rol,
                Minutos = minutos,
                FechaHora = DateTime.UtcNow
            };

            _context.RegistrosHoras.Add(registro);
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Tareas",
                registroAfectadoId: tareaId.ToString(),
                valorNuevo: $"Horas registradas como {rol.ToString().ToLower()}: {dto.Horas ?? 0}h {dto.Minutos ?? 0}min");

            await _context.SaveChangesAsync();
        }

        public async Task AprobarEntregable(int entregableId, RevisarEntregableDTO dto, string usuarioActualId)
            => await RevisarEntregable(entregableId, ResultadoRevision.Aprobada, dto, usuarioActualId);

        public async Task DevolverEntregable(int entregableId, RevisarEntregableDTO dto, string usuarioActualId)
            => await RevisarEntregable(entregableId, ResultadoRevision.Devuelta, dto, usuarioActualId);

        private async Task RevisarEntregable(int entregableId, ResultadoRevision resultado, RevisarEntregableDTO dto, string usuarioActualId)
        {
            var (tarea, mensaje) = await _transaccionService.EjecutarAsync(() => RevisarEntregableInterno(entregableId, resultado, dto, usuarioActualId));

            try
            {
                await NotificarCambioEstadoTareaAsync(tarea, usuarioActualId, mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo notificar la revisión del entregable {EntregableId}.", entregableId);
            }
        }

        private async Task<(Tarea Tarea, string Mensaje)> RevisarEntregableInterno(int entregableId, ResultadoRevision resultado, RevisarEntregableDTO dto, string usuarioActualId)
        {
            var entregable = await _context.Entregables
                .Include(en => en.Tarea).ThenInclude(t => t.Expediente)
                .FirstOrDefaultAsync(en => en.EntregableId == entregableId)
                ?? throw new ReglaNegocioException("El entregable indicado no existe.");

            var tarea = entregable.Tarea;

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden revisar entregables de expedientes abiertos.");

            if (tarea.Estado != EstadoTarea.ListaRevision)
                throw new ReglaNegocioException("Solo se puede revisar una tarea que esté lista para revisión.");

            // RF-008: el Administrador revisa cualquier expediente; el Abogado/Asesor solo
            // los de su propia cartera (donde es el responsable asignado).
            if (!await PuedeGestionarTareaAjenaAsync(usuarioActualId) && tarea.Expediente.ResponsableId != usuarioActualId)
                throw new ReglaNegocioException("Solo el responsable del expediente puede revisar sus entregables.");

            // RF-008: por defecto nadie puede aprobar o devolver el entregable de su propia
            // tarea (equivale a que nadie lo revise); el permiso expedientes.gestionar_tareas_propias
            // permite excepciones puntuales (ej. un despacho pequeño donde el Abogado/Asesor
            // también ejecuta tareas y no hay nadie más para revisarlo).
            if (tarea.ColaboradorResponsableId == usuarioActualId && !await PuedeGestionarTareasPropiasAsync(usuarioActualId))
                throw new ReglaNegocioException("No puede aprobar o devolver el entregable de su propia tarea.");

            if (resultado == ResultadoRevision.Devuelta && string.IsNullOrWhiteSpace(dto.Observaciones))
                throw new ReglaNegocioException("Debe ingresar observaciones para devolver el entregable.");

            var minutosRevision = await _context.RegistrosHoras
                .Where(r => r.TareaId == tarea.TareaId && r.RondaRevision == entregable.RondaRevision && r.Rol == RolHoras.Revisor)
                .SumAsync(r => (int?)r.Minutos) ?? 0;

            if (minutosRevision <= 0)
                throw new ReglaNegocioException("Debe registrar tiempo de revisión (con el botón + de horas) antes de aprobar o devolver.");

            var horasRevision = Math.Round(minutosRevision / 60m, 2);

            var archivo = dto.Archivo;
            if (archivo is not null)
            {
                if (archivo.Length > TamanoMaximoArchivoBytes)
                    throw new ReglaNegocioException("El archivo de la revisión no puede superar los 20 MB.");

                var extension = Path.GetExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
                    throw new ReglaNegocioException("El tipo de archivo de la revisión no está permitido.");
            }

            var revision = new RevisionEntregable
            {
                EntregableId = entregableId,
                RevisorId = usuarioActualId,
                Resultado = resultado,
                HorasRevision = horasRevision,
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones,
                FechaRevision = DateTime.UtcNow
            };

            _context.RevisionesEntregable.Add(revision);
            await _context.SaveChangesAsync();

            if (archivo is not null)
            {
                var nombreArchivo = Path.GetFileName(archivo.FileName);
                var carpetaRevision = Path.Combine(ObtenerCarpetaEntregables(), tarea.TareaId.ToString(), "revisiones", revision.RevisionId.ToString());
                Directory.CreateDirectory(carpetaRevision);

                var rutaFisica = Path.Combine(carpetaRevision, nombreArchivo);
                _transaccionService.AlRevertir(() =>
                {
                    if (File.Exists(rutaFisica))
                        File.Delete(rutaFisica);
                });
                using (var destino = File.Create(rutaFisica))
                {
                    await archivo.CopyToAsync(destino);
                }

                revision.ArchivoAdjunto = $"{tarea.TareaId}/revisiones/{revision.RevisionId}/{nombreArchivo}";
            }

            tarea.Estado = resultado == ResultadoRevision.Aprobada ? EstadoTarea.Aprobada : EstadoTarea.Devuelta;

            if (resultado == ResultadoRevision.Aprobada)
                entregable.TipoEntregable = TipoEntregable.Final;

            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: resultado == ResultadoRevision.Aprobada ? "aprobar" : "rechazar",
                moduloAfectado: "Tareas",
                registroAfectadoId: tarea.TareaId.ToString(),
                valorAnterior: "lista_revision",
                valorNuevo: resultado == ResultadoRevision.Aprobada
                    ? $"aprobada (horas revisión ronda {entregable.RondaRevision}: {horasRevision}h)"
                    : $"devuelta (horas revisión ronda {entregable.RondaRevision}: {horasRevision}h) — {dto.Observaciones}");

            await _context.SaveChangesAsync();

            var mensaje = resultado == ResultadoRevision.Aprobada
                ? $"La tarea '{tarea.Descripcion}' fue aprobada."
                : $"La tarea '{tarea.Descripcion}' fue devuelta: {dto.Observaciones}";

            return (tarea, mensaje);
        }

        public async Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoRevision(int revisionId)
        {
            var revision = await _context.RevisionesEntregable
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RevisionId == revisionId);

            if (revision?.ArchivoAdjunto is null)
                return null;

            var rutaAbsoluta = Path.Combine(ObtenerCarpetaEntregables(), revision.ArchivoAdjunto.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(rutaAbsoluta))
                return null;

            var nombreArchivo = Path.GetFileName(rutaAbsoluta);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(nombreArchivo, out var contentType))
                contentType = "application/octet-stream";

            return (rutaAbsoluta, nombreArchivo, contentType);
        }

        public async Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoEntregable(int archivoId)
        {
            var archivo = await _context.EntregableArchivos
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EntregableArchivoId == archivoId);

            if (archivo is null)
                return null;

            var rutaAbsoluta = Path.Combine(ObtenerCarpetaEntregables(), archivo.ArchivoRuta.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(rutaAbsoluta))
                return null;

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(archivo.NombreOriginal, out var contentType))
                contentType = "application/octet-stream";

            return (rutaAbsoluta, archivo.NombreOriginal, contentType);
        }
    }
}
