using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class EvaluacionCalidadService : IEvaluacionCalidadService
    {
        private const int LongitudResumenComentario = 80;

        private readonly ApplicationDbContext _context;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public EvaluacionCalidadService(ApplicationDbContext context, IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<List<EvaluacionCalidadListaDTO>> Listar()
        {
            return await _context.EvaluacionesCalidad
                .AsNoTracking()
                .OrderByDescending(e => e.FechaEvaluacion)
                .Select(e => new EvaluacionCalidadListaDTO
                {
                    EvaluacionId = e.EvaluacionId,
                    ExpedienteId = e.ExpedienteId,
                    ClienteNombre = e.Expediente.Cliente.NombreEmpresaPersona,
                    ResponsableNombre = e.Expediente.Responsable.NombreCompleto,
                    Puntuacion = e.Puntuacion,
                    ComentarioResumen = e.Comentario == null || e.Comentario.Length <= LongitudResumenComentario
                        ? e.Comentario
                        : e.Comentario.Substring(0, LongitudResumenComentario),
                    ComentarioTieneMas = e.Comentario != null && e.Comentario.Length > LongitudResumenComentario,
                    EvaluadoPorNombre = e.EvaluadoPor.NombreCompleto,
                    FechaEvaluacion = e.FechaEvaluacion
                })
                .ToListAsync();
        }

        public async Task<EvaluacionCalidadDetalleDTO?> ObtenerDetalle(int evaluacionId)
        {
            return await _context.EvaluacionesCalidad
                .AsNoTracking()
                .Where(e => e.EvaluacionId == evaluacionId)
                .Select(e => new EvaluacionCalidadDetalleDTO
                {
                    EvaluacionId = e.EvaluacionId,
                    ExpedienteId = e.ExpedienteId,
                    PropuestaId = e.Expediente.PropuestaId,
                    ClienteNombre = e.Expediente.Cliente.NombreEmpresaPersona,
                    ResponsableNombre = e.Expediente.Responsable.NombreCompleto,
                    FechaApertura = e.Expediente.FechaApertura,
                    FechaCierre = e.Expediente.FechaCierre,
                    TotalTareas = e.Expediente.Tareas.Count,
                    Puntuacion = e.Puntuacion,
                    Comentario = e.Comentario,
                    EvaluadoPorNombre = e.EvaluadoPor.NombreCompleto,
                    FechaEvaluacion = e.FechaEvaluacion
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ExpedienteEvaluableDTO?> ObtenerExpedienteEvaluable(int expedienteId)
        {
            var expediente = await _context.Expedientes
                .AsNoTracking()
                .Where(e => e.ExpedienteId == expedienteId)
                .Select(e => new
                {
                    e.ExpedienteId,
                    e.Estado,
                    e.FechaCierre,
                    ClienteNombre = e.Cliente.NombreEmpresaPersona,
                    ResponsableNombre = e.Responsable.NombreCompleto
                })
                .FirstOrDefaultAsync();

            if (expediente is null)
                return null;

            ValidarEvaluable(expediente.Estado, await YaEvaluado(expedienteId));

            return new ExpedienteEvaluableDTO
            {
                ExpedienteId = expediente.ExpedienteId,
                ClienteNombre = expediente.ClienteNombre,
                ResponsableNombre = expediente.ResponsableNombre,
                FechaCierre = expediente.FechaCierre
            };
        }

        public async Task Registrar(int expedienteId, EvaluacionCalidadCreacionDTO dto, string usuarioActualId)
        {
            var expediente = await _context.Expedientes
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExpedienteId == expedienteId)
                ?? throw new ReglaNegocioException("El expediente indicado no existe.");

            ValidarEvaluable(expediente.Estado, await YaEvaluado(expedienteId));

            if (dto.Puntuacion is null or < 1 or > 5)
                throw new ReglaNegocioException("La puntuación debe estar entre 1 y 5.");

            var comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim();

            _context.EvaluacionesCalidad.Add(new EvaluacionCalidad
            {
                ExpedienteId = expedienteId,
                Puntuacion = dto.Puntuacion.Value,
                Comentario = comentario,
                EvaluadoPorId = usuarioActualId,
                FechaEvaluacion = DateTime.UtcNow
            });

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Calidad",
                registroAfectadoId: expedienteId.ToString(),
                valorNuevo: $"Evaluación de calidad del expediente #{expedienteId}: {dto.Puntuacion}/5"
                    + (comentario is null ? string.Empty : $" — \"{comentario}\""));

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await YaEvaluado(expedienteId))
                    throw new ReglaNegocioException("Este expediente ya tiene una evaluación de calidad registrada.");

                throw;
            }
        }

        private Task<bool> YaEvaluado(int expedienteId) =>
            _context.EvaluacionesCalidad.AnyAsync(e => e.ExpedienteId == expedienteId);

        private static void ValidarEvaluable(EstadoExpediente estado, bool yaEvaluado)
        {
            if (estado != EstadoExpediente.Cerrado)
                throw new ReglaNegocioException("Solo se puede evaluar la calidad de un expediente cerrado.");

            if (yaEvaluado)
                throw new ReglaNegocioException("Este expediente ya tiene una evaluación de calidad registrada.");
        }
    }
}
