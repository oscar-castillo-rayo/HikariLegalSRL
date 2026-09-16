using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ClienteService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _userManager = userManager;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<int> ConvertirDesdeProspecto(int prospectoId, ClienteConversionDTO dto, string usuarioActualId)
        {
            var prospecto = await _context.Prospectos
                .Include(p => p.Direccion)
                .FirstOrDefaultAsync(p => p.ProspectoId == prospectoId)
                ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

            if (prospecto.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("Solo se pueden convertir a cliente los prospectos en estado activo.");

            if (dto.ModalidadPago is null)
                throw new ReglaNegocioException("Debe seleccionar la modalidad de pago.");

            if (string.IsNullOrWhiteSpace(dto.ResponsableId))
                throw new ReglaNegocioException("Debe seleccionar un Abogado/Asesor responsable.");

            var responsable = await _userManager.FindByIdAsync(dto.ResponsableId)
                ?? throw new ReglaNegocioException("El responsable indicado no existe.");

            if (!responsable.Activo)
                throw new ReglaNegocioException("El responsable seleccionado no está activo.");

            if (!await _userManager.IsInRoleAsync(responsable, RolesBase.AbogadoAsesor))
                throw new ReglaNegocioException("El responsable debe tener el rol de Abogado/Asesor.");

            var correoDuplicado = await _context.Clientes
                .AnyAsync(c => c.Correo == prospecto.Correo);

            if (correoDuplicado)
                throw new ReglaNegocioException("Ya existe un cliente registrado con ese correo electrónico.");

            var direccion = new Direccion
            {
                PaisId = prospecto.Direccion.PaisId,
                DistritoId = prospecto.Direccion.DistritoId,
                SenasExactas = prospecto.Direccion.SenasExactas,
                TipoUbicacion = prospecto.Direccion.TipoUbicacion,
                FechaCreacion = DateTime.UtcNow
            };

            var cliente = new Cliente
            {
                ProspectoOrigenId = prospecto.ProspectoId,
                NombreEmpresaPersona = prospecto.NombreEmpresaPersona,
                NombreContacto = prospecto.NombreContacto,
                CedulaJuridica = prospecto.CedulaJuridica,
                Telefono = prospecto.Telefono,
                Correo = prospecto.Correo,
                SectorEconomico = prospecto.Sector,
                Direccion = direccion,
                ModalidadPago = dto.ModalidadPago.Value,
                ResponsableId = dto.ResponsableId,
                Estado = EstadoCliente.Activo,
                FechaCreacion = DateTime.UtcNow
            };

            prospecto.Estado = EstadoProspecto.Convertido;

            _context.Clientes.Add(cliente);

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
                moduloAfectado: "Clientes",
                registroAfectadoId: cliente.ClienteId.ToString(),
                valorNuevo: $"{cliente.NombreEmpresaPersona} ({cliente.Correo})");

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Prospectos",
                registroAfectadoId: prospecto.ProspectoId.ToString(),
                valorAnterior: "activo",
                valorNuevo: "convertido");

            await _context.SaveChangesAsync();

            return cliente.ClienteId;
        }

        public async Task<List<ClienteListaDTO>> Listar(string? buscar)
        {
            var query = _context.Clientes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                query = query.Where(c =>
                    EF.Functions.Like(c.NombreEmpresaPersona, $"%{termino}%") ||
                    EF.Functions.Like(c.NombreContacto ?? "", $"%{termino}%") ||
                    EF.Functions.Like(c.Correo, $"%{termino}%"));
            }

            return await query
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => new ClienteListaDTO
                {
                    Id = c.ClienteId,
                    NombreEmpresaPersona = c.NombreEmpresaPersona,
                    NombreContacto = c.NombreContacto,
                    Correo = c.Correo,
                    Telefono = c.Telefono,
                    ModalidadPago = c.ModalidadPago,
                    Estado = c.Estado,
                    ResponsableNombre = c.Responsable.NombreCompleto,
                    FechaCreacion = c.FechaCreacion
                })
                .ToListAsync();
        }

        public async Task<ClienteDetalleDTO?> ObtenerDetalle(int id)
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.ClienteId == id)
                .Select(c => new ClienteDetalleDTO
                {
                    Id = c.ClienteId,
                    ProspectoOrigenId = c.ProspectoOrigenId,
                    NombreEmpresaPersona = c.NombreEmpresaPersona,
                    NombreContacto = c.NombreContacto,
                    CedulaJuridica = c.CedulaJuridica,
                    Telefono = c.Telefono,
                    Correo = c.Correo,
                    SectorEconomico = c.SectorEconomico,
                    ModalidadPago = c.ModalidadPago,
                    ResponsableNombre = c.Responsable.NombreCompleto,
                    Estado = c.Estado,
                    FechaCreacion = c.FechaCreacion,
                    TipoUbicacion = c.Direccion.TipoUbicacion,
                    Pais = c.Direccion.Pais.Nombre,
                    Provincia = c.Direccion.Distrito != null ? c.Direccion.Distrito.Canton.Provincia.Nombre : null,
                    Canton = c.Direccion.Distrito != null ? c.Direccion.Distrito.Canton.Nombre : null,
                    Distrito = c.Direccion.Distrito != null ? c.Direccion.Distrito.Nombre : null,
                    SenasExactas = c.Direccion.SenasExactas
                })
                .FirstOrDefaultAsync();
        }
    }
}
