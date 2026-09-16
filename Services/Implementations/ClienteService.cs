using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Clientes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public ClienteService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPermisoEvaluador permisoEvaluador,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _userManager = userManager;
            _permisoEvaluador = permisoEvaluador;
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

            await ValidarResponsable(dto.ResponsableId);

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

        public async Task<ClienteEditViewModel?> ObtenerParaEditar(int id)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .Include(c => c.Direccion)
                .FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente is null)
                return null;

            if (cliente.Estado != EstadoCliente.Activo)
                throw new ReglaNegocioException("Solo se pueden editar clientes en estado activo.");

            int? provinciaId = null;
            int? cantonId = null;

            if (cliente.Direccion.DistritoId is not null)
            {
                var ubicacion = await _context.Distritos
                    .AsNoTracking()
                    .Where(d => d.DistritoId == cliente.Direccion.DistritoId)
                    .Select(d => new { d.CantonId, d.Canton.ProvinciaId })
                    .FirstOrDefaultAsync();

                if (ubicacion is not null)
                {
                    cantonId = ubicacion.CantonId;
                    provinciaId = ubicacion.ProvinciaId;
                }
            }

            return new ClienteEditViewModel
            {
                Id = cliente.ClienteId,
                EsNacional = cliente.Direccion.TipoUbicacion == "nacional",
                ProvinciaIdActual = provinciaId,
                CantonIdActual = cantonId,
                Cliente = new ClienteEdicionDTO
                {
                    NombreEmpresaPersona = cliente.NombreEmpresaPersona,
                    NombreContacto = cliente.NombreContacto,
                    CedulaJuridica = cliente.CedulaJuridica,
                    Telefono = cliente.Telefono,
                    Correo = cliente.Correo,
                    SectorEconomico = cliente.SectorEconomico,
                    ModalidadPago = cliente.ModalidadPago,
                    Direccion = new DireccionCreacionDTO
                    {
                        PaisId = cliente.Direccion.PaisId,
                        DistritoId = cliente.Direccion.DistritoId,
                        SenasExactas = cliente.Direccion.SenasExactas
                    }
                }
            };
        }

        public async Task Editar(int id, ClienteEdicionDTO dto, string usuarioActualId)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Direccion)
                .FirstOrDefaultAsync(c => c.ClienteId == id)
                ?? throw new ReglaNegocioException("El cliente indicado no existe.");

            if (cliente.Estado != EstadoCliente.Activo)
                throw new ReglaNegocioException("Solo se pueden editar clientes en estado activo.");

            if (dto.ModalidadPago is null)
                throw new ReglaNegocioException("Debe seleccionar la modalidad de pago.");

            var correoDuplicado = await _context.Clientes
                .AnyAsync(c => c.Correo == dto.Correo && c.ClienteId != id);

            if (correoDuplicado)
                throw new ReglaNegocioException("Ya existe un cliente registrado con ese correo electrónico.");

            if (dto.Direccion.PaisId is null or < 1)
                throw new ReglaNegocioException("Debe seleccionar un país.");

            var pais = await _context.Paises.FindAsync(dto.Direccion.PaisId.Value)
                ?? throw new ReglaNegocioException("El país indicado no es válido");

            if (dto.Direccion.DistritoId is 0)
                dto.Direccion.DistritoId = null;

            var esNacional = pais.EsPaisBase;

            if (esNacional && dto.Direccion.DistritoId is null)
                throw new ReglaNegocioException("Debe indicar provincia, cantón y distrito para una dirección nacional.");

            var valorAnterior = $"{cliente.NombreEmpresaPersona} ({cliente.Correo})";

            cliente.NombreEmpresaPersona = dto.NombreEmpresaPersona;
            cliente.NombreContacto = dto.NombreContacto;
            cliente.CedulaJuridica = dto.CedulaJuridica;
            cliente.Telefono = dto.Telefono;
            cliente.Correo = dto.Correo;
            cliente.SectorEconomico = dto.SectorEconomico;
            cliente.ModalidadPago = dto.ModalidadPago.Value;

            cliente.Direccion.PaisId = dto.Direccion.PaisId.Value;
            cliente.Direccion.DistritoId = esNacional ? dto.Direccion.DistritoId : null;
            cliente.Direccion.SenasExactas = dto.Direccion.SenasExactas;
            cliente.Direccion.TipoUbicacion = esNacional ? "nacional" : "extranjero";

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
                moduloAfectado: "Clientes",
                registroAfectadoId: cliente.ClienteId.ToString(),
                valorAnterior: valorAnterior,
                valorNuevo: $"{cliente.NombreEmpresaPersona} ({cliente.Correo})");

            await _context.SaveChangesAsync();
        }

        public async Task Desactivar(int id, string usuarioActualId)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == id)
                ?? throw new ReglaNegocioException("El cliente indicado no existe.");

            if (cliente.Estado == EstadoCliente.Inactivo)
                throw new ReglaNegocioException("El cliente ya está inactivo.");

            cliente.Estado = EstadoCliente.Inactivo;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Clientes",
                registroAfectadoId: id.ToString(),
                valorAnterior: "activo",
                valorNuevo: "inactivo");

            await _context.SaveChangesAsync();
        }

        public async Task Reactivar(int id, string usuarioActualId)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == id)
                ?? throw new ReglaNegocioException("El cliente indicado no existe.");

            if (cliente.Estado != EstadoCliente.Inactivo)
                throw new ReglaNegocioException("Solo se puede reactivar un cliente inactivo.");

            cliente.Estado = EstadoCliente.Activo;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Clientes",
                registroAfectadoId: id.ToString(),
                valorAnterior: "inactivo",
                valorNuevo: "activo");

            await _context.SaveChangesAsync();
        }

        public async Task ReasignarResponsable(int id, string? nuevoResponsableId, string usuarioActualId)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Responsable)
                .FirstOrDefaultAsync(c => c.ClienteId == id)
                ?? throw new ReglaNegocioException("El cliente indicado no existe.");

            if (cliente.Estado != EstadoCliente.Activo)
                throw new ReglaNegocioException("Solo se puede reasignar el responsable de un cliente activo.");

            var nuevoResponsable = await ValidarResponsable(nuevoResponsableId);

            if (cliente.ResponsableId == nuevoResponsable.Id)
                throw new ReglaNegocioException("El cliente ya tiene asignado a ese responsable.");

            var responsableAnterior = cliente.Responsable.NombreCompleto;

            cliente.ResponsableId = nuevoResponsable.Id;
            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "editar",
                moduloAfectado: "Clientes",
                registroAfectadoId: id.ToString(),
                valorAnterior: $"Responsable: {responsableAnterior}",
                valorNuevo: $"Responsable: {nuevoResponsable.NombreCompleto}");

            await _context.SaveChangesAsync();
        }

        private async Task<ApplicationUser> ValidarResponsable(string? responsableId)
        {
            if (string.IsNullOrWhiteSpace(responsableId))
                throw new ReglaNegocioException("Debe seleccionar un responsable.");

            var responsable = await _userManager.FindByIdAsync(responsableId)
                ?? throw new ReglaNegocioException("El responsable indicado no existe.");

            if (!responsable.Activo)
                throw new ReglaNegocioException("El responsable seleccionado no está activo.");

            if (!await _permisoEvaluador.TienePermisoAsync(responsable, Permisos.Clientes.SerResponsable))
                throw new ReglaNegocioException("El responsable seleccionado no tiene permiso para gestionar clientes.");

            return responsable;
        }
    }
}
