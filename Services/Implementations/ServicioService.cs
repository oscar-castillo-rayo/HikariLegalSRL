using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Servicios;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ServicioService : IServicioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ServicioService(
            ApplicationDbContext context,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<List<ServicioListaDTO>> Listar(string? buscar)
        {
            var query = _context.CatalogoServicios.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Nombre, $"%{termino}%") ||
                    EF.Functions.Like(s.AreaCategoria, $"%{termino}%"));
            }

            return await query
                .OrderBy(s => s.Nombre)
                .Select(s => new ServicioListaDTO
                {
                    Id = s.ServicioId,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    AreaCategoria = s.AreaCategoria,
                    PrecioBase = s.PrecioBase,
                    TipoServicio = s.TipoServicio,
                    Estado = s.Estado
                })
                .ToListAsync();
        }

        public async Task<ServicioEditViewModel?> ObtenerParaEditar(int id)
        {
            var servicio = await _context.CatalogoServicios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ServicioId == id);

            if (servicio is null)
                return null;

            return new ServicioEditViewModel
            {
                Id = servicio.ServicioId,
                Servicio = new ServicioEdicionDTO
                {
                    Nombre = servicio.Nombre,
                    AreaCategoria = servicio.AreaCategoria,
                    Descripcion = servicio.Descripcion,
                    PrecioBase = servicio.PrecioBase,
                    TipoServicio = servicio.TipoServicio
                }
            };
        }

        public async Task<int> Crear(ServicioCreacionDTO dto, string usuarioActualId)
        {
            var servicio = new CatalogoServicio
            {
                Nombre = dto.Nombre,
                AreaCategoria = dto.AreaCategoria,
                Descripcion = dto.Descripcion,
                PrecioBase = dto.PrecioBase,
                TipoServicio = dto.TipoServicio!.Value,
                Estado = EstadoServicio.Activo
            };

            _context.CatalogoServicios.Add(servicio);

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
                moduloAfectado: "Servicios",
                registroAfectadoId: servicio.ServicioId.ToString(),
                valorNuevo: servicio.Nombre);

            await _context.SaveChangesAsync();

            return servicio.ServicioId;
        }

        public async Task Editar(int id, ServicioEdicionDTO dto, string usuarioActualId)
        {
            var servicio = await _context.CatalogoServicios
                .FirstOrDefaultAsync(s => s.ServicioId == id)
                ?? throw new ReglaNegocioException("El servicio indicado no existe.");

            var valorAnterior = servicio.Nombre;

            servicio.Nombre = dto.Nombre;
            servicio.AreaCategoria = dto.AreaCategoria;
            servicio.Descripcion = dto.Descripcion;
            servicio.PrecioBase = dto.PrecioBase;
            servicio.TipoServicio = dto.TipoServicio!.Value;

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
                moduloAfectado: "Servicios",
                registroAfectadoId: servicio.ServicioId.ToString(),
                valorAnterior: valorAnterior,
                valorNuevo: servicio.Nombre);

            await _context.SaveChangesAsync();
        }

        public async Task Desactivar(int id, string usuarioActualId)
        {
            var servicio = await _context.CatalogoServicios
                .FirstOrDefaultAsync(s => s.ServicioId == id)
                ?? throw new ReglaNegocioException("El servicio indicado no existe.");

            if (servicio.Estado == EstadoServicio.Inactivo)
                throw new ReglaNegocioException("El servicio ya está inactivo.");

            servicio.Estado = EstadoServicio.Inactivo;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Servicios",
                registroAfectadoId: id.ToString(),
                valorAnterior: "activo",
                valorNuevo: "inactivo");

            await _context.SaveChangesAsync();
        }

        public async Task Reactivar(int id, string usuarioActualId)
        {
            var servicio = await _context.CatalogoServicios
                .FirstOrDefaultAsync(s => s.ServicioId == id)
                ?? throw new ReglaNegocioException("El servicio indicado no existe.");

            if (servicio.Estado != EstadoServicio.Inactivo)
                throw new ReglaNegocioException("Solo se puede reactivar un servicio inactivo.");

            servicio.Estado = EstadoServicio.Activo;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Servicios",
                registroAfectadoId: id.ToString(),
                valorAnterior: "inactivo",
                valorNuevo: "activo");

            await _context.SaveChangesAsync();
        }
    }
}
