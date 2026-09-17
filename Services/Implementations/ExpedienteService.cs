using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Expedientes;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ExpedienteService : IExpedienteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ExpedienteService(
            ApplicationDbContext context,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
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
                            ColaboradorNombre = t.ColaboradorResponsable.NombreCompleto,
                            FechaLimite = t.FechaLimite,
                            HorasEstimadas = t.HorasEstimadas,
                            Prioridad = t.Prioridad,
                            Estado = t.Estado,
                            FechaCreacion = t.FechaCreacion
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

            var tarea = new Tarea
            {
                ExpedienteId = expedienteId,
                Descripcion = dto.Descripcion,
                ColaboradorResponsableId = dto.ColaboradorResponsableId!,
                FechaLimite = dto.FechaLimite!.Value,
                HorasEstimadas = dto.HorasEstimadas!.Value,
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
    }
}
