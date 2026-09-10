using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Prospectos;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ProspectoService : IProspectoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ProspectoService(ApplicationDbContext context, IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<int> Crear(ProspectoCreacionDTO dto, string usuarioActualId)
        {
            var correoDuplicado = await _context.Prospectos
                .AnyAsync(p => p.Correo == dto.Correo);

            if (correoDuplicado)
            {
                throw new ReglaNegocioException("Ya existe un prospecto registrado con ese correo electrónico.");
            }

            if (dto.Calificacion.HasValue && (dto.Calificacion < 1 || dto.Calificacion > 5))
            {
                throw new ReglaNegocioException("La calificacion debe estar entre 1 y 5.");
            }

            if (dto.Direccion.PaisId is null or < 1)
            {
                throw new ReglaNegocioException("Debe seleccionar un país.");
            }

            var pais = await _context.Paises.FindAsync(dto.Direccion.PaisId.Value);
            if (pais is null)
            {
                throw new ReglaNegocioException("El país indicado no es válido");
            }

            // Normalizar: DistritoId = 0 no es un ID válido, tratar como null
            if (dto.Direccion.DistritoId is 0)
            {
                dto.Direccion.DistritoId = null;
            }

            var esNacional = pais.EsPaisBase;

            if (esNacional && dto.Direccion.DistritoId is null)
            {
                throw new ReglaNegocioException("Debe indicar provincia, cantón y distrito para una dirección nacional.");
            }

            var direccion = new Direccion
            {
                PaisId = dto.Direccion.PaisId.Value,
                DistritoId = dto.Direccion.DistritoId,
                SenasExactas = dto.Direccion.SenasExactas,
                TipoUbicacion = esNacional ? "nacional" : "extranjero",
                FechaCreacion = DateTime.UtcNow
            };

            var prospecto = new Prospecto
            {
                NombreEmpresaPersona = dto.NombreEmpresaPersona,
                NombreContacto = dto.NombreContacto,
                CedulaJuridica = dto.CedulaJuridica,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                Sector = dto.Sector,
                Calificacion = dto.Calificacion,
                Observaciones = dto.Observaciones,
                Estado = EstadoProspecto.Activo,
                UsuarioCreadorId = usuarioActualId,
                FechaCreacion = DateTime.UtcNow,
                Direccion = direccion
            };

            _context.Prospectos.Add(prospecto);


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
                registroAfectadoId: prospecto.ProspectoId.ToString(),
                valorNuevo: $"{prospecto.NombreEmpresaPersona} ({prospecto.Correo})");

            await _context.SaveChangesAsync();  // guarda la entrada de bitácora

            return prospecto.ProspectoId;
        }

        public async Task<List<ProspectoListaDTO>> Listar(string? buscar, byte? calificacion, EstadoProspecto? estado)
        {
            var query = _context.Prospectos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                query = query.Where(p =>
                    EF.Functions.Like(p.NombreEmpresaPersona, $"%{termino}%") ||
                    EF.Functions.Like(p.NombreContacto, $"%{termino}%") ||
                    EF.Functions.Like(p.Correo, $"%{termino}%"));
            }

            if (calificacion is >= 1 and <= 5)
                query = query.Where(p => p.Calificacion == calificacion);

            if (estado is not null)
                query = query.Where(p => p.Estado == estado);

            return await query
                .OrderByDescending(p => p.FechaCreacion)
                .Select(p => new ProspectoListaDTO
                {
                    Id = p.ProspectoId,
                    NombreEmpresaPersona = p.NombreEmpresaPersona,
                    NombreContacto = p.NombreContacto,
                    Correo = p.Correo,
                    Telefono = p.Telefono,
                    Calificacion = p.Calificacion,
                    Estado = p.Estado,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();
        }

        public async Task<ProspectoDetalleDTO?> ObtenerDetalle(int id)
        {
            return await _context.Prospectos
                .AsNoTracking()
                .Where(p => p.ProspectoId == id)
                .Select(p => new ProspectoDetalleDTO
                {
                    Id = p.ProspectoId,
                    NombreEmpresaPersona = p.NombreEmpresaPersona,
                    NombreContacto = p.NombreContacto,
                    CedulaJuridica = p.CedulaJuridica,
                    Telefono = p.Telefono,
                    Correo = p.Correo,
                    Sector = p.Sector,
                    Calificacion = p.Calificacion,
                    Observaciones = p.Observaciones,
                    Estado = p.Estado,
                    FechaCreacion = p.FechaCreacion,
                    CreadoPor = p.UsuarioCreador.NombreCompleto,
                    TipoUbicacion = p.Direccion.TipoUbicacion,
                    Pais = p.Direccion.Pais.Nombre,
                    Provincia = p.Direccion.Distrito != null ? p.Direccion.Distrito.Canton.Provincia.Nombre : null,
                    Canton = p.Direccion.Distrito != null ? p.Direccion.Distrito.Canton.Nombre : null,
                    Distrito = p.Direccion.Distrito != null ? p.Direccion.Distrito.Nombre : null,
                    SenasExactas = p.Direccion.SenasExactas
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ProspectoEditViewModel?> ObtenerParaEditar(int id)
        {
            var prospecto = await _context.Prospectos
                .AsNoTracking()
                .Include(p => p.Direccion)
                .FirstOrDefaultAsync(p => p.ProspectoId == id);

            if (prospecto is null)
                return null;

            if (prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden editar prospectos en estado activo.");

            int? provinciaId = null;
            int? cantonId = null;

            if (prospecto.Direccion.DistritoId is not null)
            {
                var ubicacion = await _context.Distritos
                    .AsNoTracking()
                    .Where(d => d.DistritoId == prospecto.Direccion.DistritoId)
                    .Select(d => new { d.CantonId, d.Canton.ProvinciaId })
                    .FirstOrDefaultAsync();

                if (ubicacion is not null)
                {
                    cantonId = ubicacion.CantonId;
                    provinciaId = ubicacion.ProvinciaId;
                }
            }

            return new ProspectoEditViewModel
            {
                Id = prospecto.ProspectoId,
                EsNacional = prospecto.Direccion.TipoUbicacion == "nacional",
                ProvinciaIdActual = provinciaId,
                CantonIdActual = cantonId,
                Prospecto = new ProspectoEdicionDTO
                {
                    NombreEmpresaPersona = prospecto.NombreEmpresaPersona,
                    NombreContacto = prospecto.NombreContacto,
                    CedulaJuridica = prospecto.CedulaJuridica,
                    Telefono = prospecto.Telefono,
                    Correo = prospecto.Correo,
                    Sector = prospecto.Sector,
                    Calificacion = prospecto.Calificacion,
                    Observaciones = prospecto.Observaciones,
                    Direccion = new DireccionCreacionDTO
                    {
                        PaisId = prospecto.Direccion.PaisId,
                        DistritoId = prospecto.Direccion.DistritoId,
                        SenasExactas = prospecto.Direccion.SenasExactas
                    }
                }
            };
        }

        public async Task Editar(int id, ProspectoEdicionDTO dto, string usuarioActualId)
        {
            var prospecto = await _context.Prospectos
                .Include(p => p.Direccion)
                .FirstOrDefaultAsync(p => p.ProspectoId == id)
                ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

            if (prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden editar prospectos en estado activo.");

            if (dto.Calificacion.HasValue && (dto.Calificacion < 1 || dto.Calificacion > 5))
                throw new ReglaNegocioException("La calificacion debe estar entre 1 y 5.");

            var correoDuplicado = await _context.Prospectos
                .AnyAsync(p => p.Correo == dto.Correo && p.ProspectoId != id);

            if (correoDuplicado)
                throw new ReglaNegocioException("Ya existe un prospecto registrado con ese correo electrónico.");

            if (dto.Direccion.PaisId is null or < 1)
                throw new ReglaNegocioException("Debe seleccionar un país.");

            var pais = await _context.Paises.FindAsync(dto.Direccion.PaisId.Value)
                ?? throw new ReglaNegocioException("El país indicado no es válido");

            if (dto.Direccion.DistritoId is 0)
                dto.Direccion.DistritoId = null;

            var esNacional = pais.EsPaisBase;

            if (esNacional && dto.Direccion.DistritoId is null)
                throw new ReglaNegocioException("Debe indicar provincia, cantón y distrito para una dirección nacional.");

            var valorAnterior = $"{prospecto.NombreEmpresaPersona} ({prospecto.Correo})";

            prospecto.NombreEmpresaPersona = dto.NombreEmpresaPersona;
            prospecto.NombreContacto = dto.NombreContacto;
            prospecto.CedulaJuridica = dto.CedulaJuridica;
            prospecto.Telefono = dto.Telefono;
            prospecto.Correo = dto.Correo;
            prospecto.Sector = dto.Sector;
            prospecto.Calificacion = dto.Calificacion;
            prospecto.Observaciones = dto.Observaciones;

            prospecto.Direccion.PaisId = dto.Direccion.PaisId.Value;
            prospecto.Direccion.DistritoId = esNacional ? dto.Direccion.DistritoId : null;
            prospecto.Direccion.SenasExactas = dto.Direccion.SenasExactas;
            prospecto.Direccion.TipoUbicacion = esNacional ? "nacional" : "extranjero";

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
                registroAfectadoId: prospecto.ProspectoId.ToString(),
                valorAnterior: valorAnterior,
                valorNuevo: $"{prospecto.NombreEmpresaPersona} ({prospecto.Correo})");

            await _context.SaveChangesAsync();
        }

        public async Task Descartar(int id, string usuarioActualId)
        {
            var prospecto = await _context.Prospectos
                .FirstOrDefaultAsync(p => p.ProspectoId == id)
                ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

            if (prospecto.Estado == EstadoProspecto.Convertido)
                throw new ReglaNegocioException("Un prospecto convertido no puede ser descartado.");

            if (prospecto.Estado == EstadoProspecto.Descartado)
                throw new ReglaNegocioException("El prospecto ya está descartado.");

            prospecto.Estado = EstadoProspecto.Descartado;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Prospectos",
                registroAfectadoId: id.ToString(),
                valorAnterior: "activo",
                valorNuevo: "descartado");

            await _context.SaveChangesAsync();
        }

        public async Task Reactivar(int id, string usuarioActualId)
        {
            var prospecto = await _context.Prospectos
                .FirstOrDefaultAsync(p => p.ProspectoId == id)
                ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

            if (prospecto.Estado != EstadoProspecto.Descartado)
                throw new ReglaNegocioException("Solo se puede reactivar un prospecto descartado.");

            prospecto.Estado = EstadoProspecto.Activo;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Prospectos",
                registroAfectadoId: id.ToString(),
                valorAnterior: "descartado",
                valorNuevo: "activo");

            await _context.SaveChangesAsync();
        }
    }
}

