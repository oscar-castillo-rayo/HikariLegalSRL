using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
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

            var pais = await _context.Paises.FindAsync(dto.Direccion.PaisId);
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
                PaisId = dto.Direccion.PaisId,
                DistritoId = dto.Direccion.DistritoId,
                SenasExactas = dto.Direccion.SenasExactas,
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
    }
}

