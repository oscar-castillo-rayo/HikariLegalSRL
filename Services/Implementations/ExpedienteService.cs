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
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ExpedienteService(
            ApplicationDbContext context,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        private string ObtenerCarpetaEntregables()
        {
            var rutaConfigurada = _configuration["Almacenamiento:EntregablesPath"] ?? "App_Data/entregables";
            return Path.Combine(_webHostEnvironment.ContentRootPath, rutaConfigurada);
        }

        private static decimal CombinarHorasMinutos(int? horas, int? minutos)
            => Math.Round((horas ?? 0) + (minutos ?? 0) / 60m, 2);

        private async Task<bool> PuedeGestionarTareaAjenaAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Expedientes.GestionarAjenas);
        }

        public async Task<List<ExpedienteListaDTO>> Listar()
        {
            return await _context.Expedientes
                .AsNoTracking()
                .Include(e => e.Cliente)
                .Include(e => e.Responsable)
                .Include(e => e.Tareas)
                .OrderByDescending(e => e.FechaApertura)
                .Select(e => new ExpedienteListaDTO
                {
                    Id = e.ExpedienteId,
                    ClienteNombre = e.Cliente.NombreEmpresaPersona,
                    ResponsableNombre = e.Responsable.NombreCompleto,
                    PlazoComprometido = e.PlazoComprometido,
                    Estado = e.Estado,
                    TotalTareas = e.Tareas.Count,
                    TareasAprobadas = e.Tareas.Count(t => t.Estado == EstadoTarea.Aprobada)
                })
                .ToListAsync();
        }

        public async Task<ExpedienteDetalleViewModel?> ObtenerDetalle(int id)
        {
            var expediente = await _context.Expedientes
                .AsNoTracking()
                .Include(e => e.Cliente)
                .Include(e => e.Responsable)
                .Include(e => e.Tareas).ThenInclude(t => t.ColaboradorResponsable)
                .Include(e => e.Tareas).ThenInclude(t => t.Entregables)
                .FirstOrDefaultAsync(e => e.ExpedienteId == id);

            if (expediente is null)
                return null;

            return new ExpedienteDetalleViewModel
            {
                Expediente = new ExpedienteDetalleDTO
                {
                    Id = expediente.ExpedienteId,
                    ClienteNombre = expediente.Cliente.NombreEmpresaPersona,
                    PropuestaId = expediente.PropuestaId,
                    ResponsableNombre = expediente.Responsable.NombreCompleto,
                    PlazoComprometido = expediente.PlazoComprometido,
                    FechaApertura = expediente.FechaApertura,
                    FechaCierre = expediente.FechaCierre,
                    Estado = expediente.Estado,
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
                            UltimoEntregableId = t.Entregables
                                .OrderByDescending(en => en.RondaRevision)
                                .Select(en => (int?)en.EntregableId)
                                .FirstOrDefault(),
                            HorasReales = t.Entregables
                                .OrderByDescending(en => en.RondaRevision)
                                .Select(en => (decimal?)en.HorasReales)
                                .FirstOrDefault(),
                            TieneArchivoAdjunto = t.Entregables
                                .OrderByDescending(en => en.RondaRevision)
                                .Select(en => en.ArchivoRuta)
                                .FirstOrDefault() != null
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
                moduloAfectado: "Expedientes",
                registroAfectadoId: tarea.TareaId.ToString(),
                valorNuevo: $"Tarea '{tarea.Descripcion}' asignada, expediente #{expedienteId}");

            await _context.SaveChangesAsync();
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
        }

        public async Task IniciarTarea(int tareaId, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden iniciar tareas de expedientes abiertos.");

            if (tarea.Estado != EstadoTarea.Pendiente)
                throw new ReglaNegocioException("Solo se puede iniciar una tarea que esté pendiente.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede iniciar esta tarea.");

            tarea.Estado = EstadoTarea.EnProceso;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Expedientes",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: "pendiente",
                valorNuevo: "en_proceso");

            await _context.SaveChangesAsync();
        }

        public async Task MarcarListaParaRevision(int tareaId, CargarEntregableDTO dto, string usuarioActualId)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Expediente)
                .Include(t => t.Entregables)
                .FirstOrDefaultAsync(t => t.TareaId == tareaId)
                ?? throw new ReglaNegocioException("La tarea indicada no existe.");

            if (tarea.Expediente.Estado != EstadoExpediente.Abierto)
                throw new ReglaNegocioException("Solo se pueden actualizar tareas de expedientes abiertos.");

            if (tarea.Estado != EstadoTarea.EnProceso)
                throw new ReglaNegocioException("Solo se puede enviar a revisión una tarea que esté en proceso.");

            if (tarea.ColaboradorResponsableId != usuarioActualId && !await PuedeGestionarTareaAjenaAsync(usuarioActualId))
                throw new ReglaNegocioException("Solo el colaborador asignado puede completar esta tarea.");

            var horasReales = CombinarHorasMinutos(dto.Horas, dto.Minutos);
            if (horasReales <= 0)
                throw new ReglaNegocioException("Debe registrar las horas reales trabajadas antes de enviar a revisión.");

            var archivo = dto.Archivo;
            if (archivo is not null)
            {
                if (archivo.Length > TamanoMaximoArchivoBytes)
                    throw new ReglaNegocioException("El archivo del entregable no puede superar los 20 MB.");

                var extension = Path.GetExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
                    throw new ReglaNegocioException("El tipo de archivo del entregable no está permitido.");
            }

            var entregable = new Entregable
            {
                TareaId = tareaId,
                RondaRevision = tarea.Entregables.Count + 1,
                HorasReales = horasReales,
                TipoEntregable = TipoEntregable.Preliminar,
                CargadoPorId = usuarioActualId,
                FechaCarga = DateTime.UtcNow
            };

            _context.Entregables.Add(entregable);
            await _context.SaveChangesAsync();

            if (archivo is not null)
            {
                var nombreArchivo = Path.GetFileName(archivo.FileName);
                var carpetaTarea = Path.Combine(ObtenerCarpetaEntregables(), tareaId.ToString(), entregable.EntregableId.ToString());
                Directory.CreateDirectory(carpetaTarea);

                var rutaFisica = Path.Combine(carpetaTarea, nombreArchivo);
                using (var destino = File.Create(rutaFisica))
                {
                    await archivo.CopyToAsync(destino);
                }

                entregable.ArchivoRuta = $"{tareaId}/{entregable.EntregableId}/{nombreArchivo}";
            }

            tarea.Estado = EstadoTarea.ListaRevision;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Expedientes",
                registroAfectadoId: tareaId.ToString(),
                valorAnterior: "en_proceso",
                valorNuevo: $"lista_revision (horas reales: {dto.Horas ?? 0}h {dto.Minutos ?? 0}min)");

            await _context.SaveChangesAsync();
        }

        public async Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoEntregable(int entregableId)
        {
            var entregable = await _context.Entregables
                .AsNoTracking()
                .FirstOrDefaultAsync(en => en.EntregableId == entregableId);

            if (entregable?.ArchivoRuta is null)
                return null;

            var rutaAbsoluta = Path.Combine(ObtenerCarpetaEntregables(), entregable.ArchivoRuta.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(rutaAbsoluta))
                return null;

            var nombreArchivo = Path.GetFileName(rutaAbsoluta);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(nombreArchivo, out var contentType))
                contentType = "application/octet-stream";

            return (rutaAbsoluta, nombreArchivo, contentType);
        }
    }
}
