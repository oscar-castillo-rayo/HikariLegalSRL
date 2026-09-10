using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ActividadSeguimientoService : IActividadSeguimientoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ActividadSeguimientoService(ApplicationDbContext context, IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<List<ActividadSeguimientoDTO>> Listar(int prospectoId)
        {
            return await _context.ActividadesSeguimiento
                .AsNoTracking()
                .Where(a => a.ProspectoId == prospectoId)
                .OrderByDescending(a => a.FechaHora)
                .Select(a => new ActividadSeguimientoDTO
                {
                    Id = a.ActividadSeguimientoId,
                    TipoActividad = a.TipoActividad,
                    Titulo = a.Titulo,
                    Descripcion = a.Descripcion,
                    FechaHora = a.FechaHora,
                    RegistradoPor = a.UsuarioRegistro.NombreCompleto,
                    ResponsableId = a.ResponsableId,
                    ResponsableNombre = a.Responsable.NombreCompleto
                })
                .ToListAsync();
        }

        public async Task<ActividadSeguimientoDTO?> Obtener(int actividadId)
        {
            return await _context.ActividadesSeguimiento
                .AsNoTracking()
                .Where(a => a.ActividadSeguimientoId == actividadId)
                .Select(a => new ActividadSeguimientoDTO
                {
                    Id = a.ActividadSeguimientoId,
                    TipoActividad = a.TipoActividad,
                    Titulo = a.Titulo,
                    Descripcion = a.Descripcion,
                    FechaHora = a.FechaHora,
                    RegistradoPor = a.UsuarioRegistro.NombreCompleto,
                    ResponsableId = a.ResponsableId,
                    ResponsableNombre = a.Responsable.NombreCompleto
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> Registrar(int prospectoId, ActividadSeguimientoFormDTO dto, string usuarioActualId)
        {
            var prospecto = await _context.Prospectos
                .FirstOrDefaultAsync(p => p.ProspectoId == prospectoId)
                ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

            if (prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden registrar actividades en prospectos activos.");

            await ValidarResponsable(dto.ResponsableId);

            var actividad = new ActividadSeguimiento
            {
                ProspectoId = prospectoId,
                TipoActividad = dto.Tipo,
                Titulo = dto.Titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                FechaHora = dto.FechaHora.ToUniversalTime(),
                UsuarioRegistroId = usuarioActualId,
                ResponsableId = dto.ResponsableId
            };

            _context.ActividadesSeguimiento.Add(actividad);

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
                moduloAfectado: "Prospectos",
                registroAfectadoId: actividad.ActividadSeguimientoId.ToString(),
                valorNuevo: Resumen(actividad.TipoActividad, actividad.Titulo));

            await _context.SaveChangesAsync();

            return actividad.ActividadSeguimientoId;
        }

        public async Task Editar(int actividadId, ActividadSeguimientoFormDTO dto, string usuarioActualId)
        {
            var actividad = await _context.ActividadesSeguimiento
                .Include(a => a.Prospecto)
                .FirstOrDefaultAsync(a => a.ActividadSeguimientoId == actividadId)
                ?? throw new ReglaNegocioException("La actividad indicada no existe.");

            if (actividad.Prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden editar actividades de prospectos activos.");

            await ValidarResponsable(dto.ResponsableId);

            var valorAnterior = Resumen(actividad.TipoActividad, actividad.Titulo);

            actividad.TipoActividad = dto.Tipo;
            actividad.Titulo = dto.Titulo.Trim();
            actividad.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            actividad.FechaHora = dto.FechaHora.ToUniversalTime();
            actividad.ResponsableId = dto.ResponsableId;

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
                moduloAfectado: "Prospectos",
                registroAfectadoId: actividad.ActividadSeguimientoId.ToString(),
                valorAnterior: valorAnterior,
                valorNuevo: Resumen(actividad.TipoActividad, actividad.Titulo));

            await _context.SaveChangesAsync();
        }

        public async Task Eliminar(int actividadId, string usuarioActualId)
        {
            var actividad = await _context.ActividadesSeguimiento
                .Include(a => a.Prospecto)
                .FirstOrDefaultAsync(a => a.ActividadSeguimientoId == actividadId)
                ?? throw new ReglaNegocioException("La actividad indicada no existe.");

            if (actividad.Prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden eliminar actividades de prospectos activos.");

            var valorAnterior = Resumen(actividad.TipoActividad, actividad.Titulo);

            _context.ActividadesSeguimiento.Remove(actividad);

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
                tipoAccion: "eliminar",
                moduloAfectado: "Prospectos",
                registroAfectadoId: actividadId.ToString(),
                valorAnterior: valorAnterior);

            await _context.SaveChangesAsync();
        }

        private async Task ValidarResponsable(string responsableId)
        {
            var responsableValido = await _context.Users
                .AnyAsync(u => u.Id == responsableId && u.Activo);

            if (!responsableValido)
                throw new ReglaNegocioException("El responsable indicado no es un usuario activo válido.");
        }

        private static string Resumen(TipoActividad tipo, string titulo) => $"{tipo}: {titulo}";
    }
}
