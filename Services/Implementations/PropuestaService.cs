using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Propuestas;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class PropuestaService : IPropuestaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IClienteService _clienteService;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public PropuestaService(
            ApplicationDbContext context,
            IClienteService clienteService,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _context = context;
            _clienteService = clienteService;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        public async Task<List<OpcionComboDTO>> ObtenerProspectosActivos()
        {
            return await _context.Prospectos
                .AsNoTracking()
                .Where(p => p.Estado == EstadoProspecto.Activo)
                .OrderBy(p => p.NombreEmpresaPersona)
                .Select(p => new OpcionComboDTO { Id = p.ProspectoId, Nombre = p.NombreEmpresaPersona })
                .ToListAsync();
        }

        public async Task<List<OpcionComboDTO>> ObtenerClientesActivos()
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Estado == EstadoCliente.Activo)
                .OrderBy(c => c.NombreEmpresaPersona)
                .Select(c => new OpcionComboDTO { Id = c.ClienteId, Nombre = c.NombreEmpresaPersona })
                .ToListAsync();
        }

        public async Task<List<ServicioOpcionDTO>> ObtenerServiciosActivos()
        {
            return await _context.CatalogoServicios
                .AsNoTracking()
                .Where(s => s.Estado == EstadoServicio.Activo)
                .OrderBy(s => s.Nombre)
                .Select(s => new ServicioOpcionDTO
                {
                    Id = s.ServicioId,
                    Nombre = s.Nombre,
                    PrecioBase = s.PrecioBase,
                    TipoServicio = s.TipoServicio
                })
                .ToListAsync();
        }

        public async Task<List<PropuestaListaDTO>> Listar(string? buscar, EstadoPropuesta? estado)
        {
            var query = _context.Propuestas
                .AsNoTracking()
                .Include(p => p.Prospecto)
                .Include(p => p.Cliente)
                .Include(p => p.ElaboradaPor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                query = query.Where(p =>
                    EF.Functions.Like(p.Prospecto!.NombreEmpresaPersona, $"%{termino}%") ||
                    EF.Functions.Like(p.Cliente!.NombreEmpresaPersona, $"%{termino}%"));
            }

            if (estado is not null)
                query = query.Where(p => p.Estado == estado);

            return await query
                .OrderByDescending(p => p.FechaCreacion)
                .Select(p => new PropuestaListaDTO
                {
                    Id = p.PropuestaId,
                    Destinatario = p.ProspectoId != null ? p.Prospecto!.NombreEmpresaPersona : p.Cliente!.NombreEmpresaPersona,
                    EsProspecto = p.ProspectoId != null,
                    Moneda = p.Moneda,
                    MontoTotal = p.MontoTotal,
                    Estado = p.Estado,
                    ModalidadPago = p.ModalidadPago,
                    PlazoDias = p.PlazoDias,
                    ElaboradaPorNombre = p.ElaboradaPor.NombreCompleto,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();
        }

        public async Task<PropuestaDetalleDTO?> ObtenerDetalle(int id)
        {
            var propuesta = await _context.Propuestas
                .AsNoTracking()
                .Include(p => p.Prospecto)
                .Include(p => p.Cliente)
                .Include(p => p.ElaboradaPor)
                .Include(p => p.Servicios).ThenInclude(s => s.Servicio)
                .FirstOrDefaultAsync(p => p.PropuestaId == id);

            if (propuesta is null)
                return null;

            return new PropuestaDetalleDTO
            {
                Id = propuesta.PropuestaId,
                Destinatario = propuesta.ProspectoId != null ? propuesta.Prospecto!.NombreEmpresaPersona : propuesta.Cliente!.NombreEmpresaPersona,
                EsProspecto = propuesta.ProspectoId != null,
                Moneda = propuesta.Moneda,
                MontoTotal = propuesta.MontoTotal,
                PlazoDias = propuesta.PlazoDias,
                ModalidadPago = propuesta.ModalidadPago,
                DescripcionGeneral = propuesta.DescripcionGeneral,
                Estado = propuesta.Estado,
                ElaboradaPorNombre = propuesta.ElaboradaPor.NombreCompleto,
                FechaCreacion = propuesta.FechaCreacion,
                FechaEnvio = propuesta.FechaEnvio,
                FechaResolucion = propuesta.FechaResolucion,
                Servicios = propuesta.Servicios.Select(s => new PropuestaServicioDetalleDTO
                {
                    ServicioNombre = s.Servicio.Nombre,
                    DescripcionServicio = s.DescripcionServicio,
                    DescripcionCatalogo = s.Servicio.Descripcion,
                    Precio = s.Precio,
                    TipoServicio = s.TipoServicio
                }).ToList()
            };
        }

        public async Task<int> Crear(PropuestaCreacionDTO dto, string usuarioActualId)
        {
            var tieneProspecto = dto.ProspectoId.HasValue && dto.ProspectoId.Value > 0;
            var tieneCliente = dto.ClienteId.HasValue && dto.ClienteId.Value > 0;

            if (tieneProspecto == tieneCliente)
                throw new ReglaNegocioException("Debe seleccionar un prospecto o un cliente, no ambos.");

            if (dto.Servicios.Count == 0)
                throw new ReglaNegocioException("Debe agregar al menos un servicio a la propuesta.");

            if (tieneProspecto)
            {
                var prospecto = await _context.Prospectos.FindAsync(dto.ProspectoId!.Value)
                    ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

                if (prospecto.Estado != EstadoProspecto.Activo)
                    throw new ReglaNegocioException("Solo se pueden crear propuestas para prospectos activos.");
            }
            else
            {
                var cliente = await _context.Clientes.FindAsync(dto.ClienteId!.Value)
                    ?? throw new ReglaNegocioException("El cliente indicado no existe.");

                if (cliente.Estado != EstadoCliente.Activo)
                    throw new ReglaNegocioException("Solo se pueden crear propuestas para clientes activos.");
            }

            var servicioIds = dto.Servicios.Select(s => s.ServicioId!.Value).ToList();
            var serviciosCatalogo = await _context.CatalogoServicios
                .Where(s => servicioIds.Contains(s.ServicioId))
                .ToDictionaryAsync(s => s.ServicioId);

            var items = new List<PropuestaServicio>();
            foreach (var item in dto.Servicios)
            {
                if (!serviciosCatalogo.TryGetValue(item.ServicioId!.Value, out var servicio))
                    throw new ReglaNegocioException("Uno de los servicios seleccionados no existe.");

                if (servicio.Estado != EstadoServicio.Activo)
                    throw new ReglaNegocioException($"El servicio '{servicio.Nombre}' no está disponible para nuevas propuestas.");

                items.Add(new PropuestaServicio
                {
                    ServicioId = servicio.ServicioId,
                    DescripcionServicio = item.DescripcionServicio,
                    Precio = item.Precio,
                    TipoServicio = servicio.TipoServicio
                });
            }

            var propuesta = new Propuesta
            {
                ProspectoId = tieneProspecto ? dto.ProspectoId : null,
                ClienteId = tieneCliente ? dto.ClienteId : null,
                Moneda = dto.Moneda!.Value,
                MontoTotal = items.Sum(i => i.Precio),
                PlazoDias = dto.PlazoDias,
                ModalidadPago = dto.ModalidadPago!.Value,
                DescripcionGeneral = dto.DescripcionGeneral,
                Estado = EstadoPropuesta.Borrador,
                ElaboradaPorId = usuarioActualId,
                FechaCreacion = DateTime.UtcNow,
                Servicios = items
            };

            _context.Propuestas.Add(propuesta);

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
                moduloAfectado: "Propuestas",
                registroAfectadoId: propuesta.PropuestaId.ToString(),
                valorNuevo: $"Propuesta borrador por {propuesta.MontoTotal:N2}");

            await _context.SaveChangesAsync();

            return propuesta.PropuestaId;
        }

        public async Task<PropuestaEditViewModel?> ObtenerParaEditar(int id)
        {
            var propuesta = await _context.Propuestas
                .AsNoTracking()
                .Include(p => p.Servicios)
                .FirstOrDefaultAsync(p => p.PropuestaId == id);

            if (propuesta is null)
                return null;

            if (propuesta.Estado != EstadoPropuesta.Borrador)
                throw new ReglaNegocioException("Solo se pueden editar propuestas en estado borrador.");

            var prospectos = await ObtenerProspectosActivos();
            if (propuesta.ProspectoId is not null && !prospectos.Any(p => p.Id == propuesta.ProspectoId))
            {
                var prospectoActual = await _context.Prospectos
                    .AsNoTracking()
                    .Where(p => p.ProspectoId == propuesta.ProspectoId)
                    .Select(p => new OpcionComboDTO { Id = p.ProspectoId, Nombre = p.NombreEmpresaPersona })
                    .FirstOrDefaultAsync();
                if (prospectoActual is not null)
                    prospectos.Insert(0, prospectoActual);
            }

            var clientes = await ObtenerClientesActivos();
            if (propuesta.ClienteId is not null && !clientes.Any(c => c.Id == propuesta.ClienteId))
            {
                var clienteActual = await _context.Clientes
                    .AsNoTracking()
                    .Where(c => c.ClienteId == propuesta.ClienteId)
                    .Select(c => new OpcionComboDTO { Id = c.ClienteId, Nombre = c.NombreEmpresaPersona })
                    .FirstOrDefaultAsync();
                if (clienteActual is not null)
                    clientes.Insert(0, clienteActual);
            }

            var servicios = await ObtenerServiciosActivos();
            var idsUsados = propuesta.Servicios.Select(s => s.ServicioId).Distinct().ToList();
            var idsFaltantes = idsUsados.Except(servicios.Select(s => s.Id)).ToList();
            if (idsFaltantes.Count > 0)
            {
                var faltantes = await _context.CatalogoServicios
                    .AsNoTracking()
                    .Where(s => idsFaltantes.Contains(s.ServicioId))
                    .Select(s => new ServicioOpcionDTO
                    {
                        Id = s.ServicioId,
                        Nombre = s.Nombre,
                        PrecioBase = s.PrecioBase,
                        TipoServicio = s.TipoServicio
                    })
                    .ToListAsync();
                servicios.InsertRange(0, faltantes);
            }

            return new PropuestaEditViewModel
            {
                Id = propuesta.PropuestaId,
                Propuesta = new PropuestaEdicionDTO
                {
                    ProspectoId = propuesta.ProspectoId,
                    ClienteId = propuesta.ClienteId,
                    Moneda = propuesta.Moneda,
                    PlazoDias = propuesta.PlazoDias,
                    ModalidadPago = propuesta.ModalidadPago,
                    DescripcionGeneral = propuesta.DescripcionGeneral,
                    Servicios = propuesta.Servicios.Select(s => new PropuestaServicioItemDTO
                    {
                        ServicioId = s.ServicioId,
                        DescripcionServicio = s.DescripcionServicio,
                        Precio = s.Precio
                    }).ToList()
                },
                Prospectos = prospectos,
                Clientes = clientes,
                Servicios = servicios
            };
        }

        public async Task Editar(int id, PropuestaEdicionDTO dto, string usuarioActualId)
        {
            var propuesta = await _context.Propuestas
                .Include(p => p.Servicios)
                .FirstOrDefaultAsync(p => p.PropuestaId == id)
                ?? throw new ReglaNegocioException("La propuesta indicada no existe.");

            if (propuesta.Estado != EstadoPropuesta.Borrador)
                throw new ReglaNegocioException("Solo se pueden editar propuestas en estado borrador.");

            var tieneProspecto = dto.ProspectoId.HasValue && dto.ProspectoId.Value > 0;
            var tieneCliente = dto.ClienteId.HasValue && dto.ClienteId.Value > 0;

            if (tieneProspecto == tieneCliente)
                throw new ReglaNegocioException("Debe seleccionar un prospecto o un cliente, no ambos.");

            if (dto.Servicios.Count == 0)
                throw new ReglaNegocioException("Debe agregar al menos un servicio a la propuesta.");

            if (tieneProspecto)
            {
                var prospecto = await _context.Prospectos.FindAsync(dto.ProspectoId!.Value)
                    ?? throw new ReglaNegocioException("El prospecto indicado no existe.");

                if (prospecto.Estado != EstadoProspecto.Activo)
                    throw new ReglaNegocioException("Solo se pueden asignar propuestas a prospectos activos.");
            }
            else
            {
                var cliente = await _context.Clientes.FindAsync(dto.ClienteId!.Value)
                    ?? throw new ReglaNegocioException("El cliente indicado no existe.");

                if (cliente.Estado != EstadoCliente.Activo)
                    throw new ReglaNegocioException("Solo se pueden asignar propuestas a clientes activos.");
            }

            var servicioIds = dto.Servicios.Select(s => s.ServicioId!.Value).ToList();
            var serviciosCatalogo = await _context.CatalogoServicios
                .Where(s => servicioIds.Contains(s.ServicioId))
                .ToDictionaryAsync(s => s.ServicioId);

            var nuevosItems = new List<PropuestaServicio>();
            foreach (var item in dto.Servicios)
            {
                if (!serviciosCatalogo.TryGetValue(item.ServicioId!.Value, out var servicio))
                    throw new ReglaNegocioException("Uno de los servicios seleccionados no existe.");

                // A diferencia de Crear, no se exige que el servicio siga activo: la propuesta
                // pudo haberlo incluido cuando sí lo estaba, y el desplegable ya solo ofrece
                // servicios activos más los que ya estaban en esta propuesta (ver ObtenerParaEditar).
                nuevosItems.Add(new PropuestaServicio
                {
                    ServicioId = servicio.ServicioId,
                    DescripcionServicio = item.DescripcionServicio,
                    Precio = item.Precio,
                    TipoServicio = servicio.TipoServicio
                });
            }

            _context.PropuestaServicios.RemoveRange(propuesta.Servicios);

            propuesta.ProspectoId = tieneProspecto ? dto.ProspectoId : null;
            propuesta.ClienteId = tieneCliente ? dto.ClienteId : null;
            propuesta.Moneda = dto.Moneda!.Value;
            propuesta.PlazoDias = dto.PlazoDias;
            propuesta.ModalidadPago = dto.ModalidadPago!.Value;
            propuesta.DescripcionGeneral = dto.DescripcionGeneral;
            propuesta.MontoTotal = nuevosItems.Sum(i => i.Precio);
            propuesta.Servicios = nuevosItems;

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
                moduloAfectado: "Propuestas",
                registroAfectadoId: propuesta.PropuestaId.ToString(),
                valorNuevo: $"Propuesta borrador por {propuesta.MontoTotal:N2}");

            await _context.SaveChangesAsync();
        }

        public async Task MarcarComoEnviada(int id, string usuarioActualId)
        {
            var propuesta = await _context.Propuestas
                .Include(p => p.Prospecto)
                .Include(p => p.Cliente)
                .Include(p => p.Servicios)
                .FirstOrDefaultAsync(p => p.PropuestaId == id)
                ?? throw new ReglaNegocioException("La propuesta indicada no existe.");

            if (propuesta.Estado != EstadoPropuesta.Borrador)
                throw new ReglaNegocioException("Solo se pueden enviar propuestas en estado borrador.");

            if (propuesta.Servicios.Count == 0)
                throw new ReglaNegocioException("Debe agregar al menos un servicio a la propuesta antes de enviarla.");

            ValidarDestinatarioActivo(propuesta);

            propuesta.Estado = EstadoPropuesta.Enviada;
            propuesta.FechaEnvio = DateTime.UtcNow;

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
                moduloAfectado: "Propuestas",
                registroAfectadoId: propuesta.PropuestaId.ToString(),
                valorAnterior: "borrador",
                valorNuevo: "enviada");

            await _context.SaveChangesAsync();
        }

        public async Task MarcarComoAceptada(int id, string usuarioActualId)
        {
            var propuesta = await _context.Propuestas
                .Include(p => p.Prospecto)
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.PropuestaId == id)
                ?? throw new ReglaNegocioException("La propuesta indicada no existe.");

            if (propuesta.Estado != EstadoPropuesta.Enviada)
                throw new ReglaNegocioException("Solo se pueden aceptar propuestas en estado enviada.");

            ValidarDestinatarioActivo(propuesta);

            int clienteId;
            string responsableId;

            if (propuesta.ClienteId is not null)
            {
                clienteId = propuesta.ClienteId.Value;
                responsableId = propuesta.Cliente!.ResponsableId;
            }
            else
            {
                clienteId = await _clienteService.ConvertirDesdeProspecto(
                    propuesta.ProspectoId!.Value,
                    new ClienteConversionDTO
                    {
                        ModalidadPago = propuesta.ModalidadPago,
                        ResponsableId = propuesta.ElaboradaPorId
                    },
                    usuarioActualId);
                responsableId = propuesta.ElaboradaPorId;
            }

            propuesta.Estado = EstadoPropuesta.Aceptada;
            propuesta.FechaResolucion = DateTime.UtcNow;

            var expediente = new Expediente
            {
                ClienteId = clienteId,
                PropuestaId = propuesta.PropuestaId,
                ResponsableId = responsableId,
                PlazoComprometido = DateTime.UtcNow.Date.AddDays(propuesta.PlazoDias),
                FechaApertura = DateTime.UtcNow,
                Estado = EstadoExpediente.Abierto
            };

            _context.Expedientes.Add(expediente);

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
                moduloAfectado: "Propuestas",
                registroAfectadoId: propuesta.PropuestaId.ToString(),
                valorAnterior: "enviada",
                valorNuevo: "aceptada");

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Expedientes",
                registroAfectadoId: expediente.ExpedienteId.ToString(),
                valorNuevo: $"Expediente abierto desde propuesta #{propuesta.PropuestaId}");

            await _context.SaveChangesAsync();
        }

        public async Task MarcarComoRechazada(int id, string usuarioActualId)
        {
            var propuesta = await _context.Propuestas
                .FirstOrDefaultAsync(p => p.PropuestaId == id)
                ?? throw new ReglaNegocioException("La propuesta indicada no existe.");

            if (propuesta.Estado != EstadoPropuesta.Enviada)
                throw new ReglaNegocioException("Solo se pueden rechazar propuestas en estado enviada.");

            propuesta.Estado = EstadoPropuesta.Rechazada;
            propuesta.FechaResolucion = DateTime.UtcNow;

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
                moduloAfectado: "Propuestas",
                registroAfectadoId: propuesta.PropuestaId.ToString(),
                valorAnterior: "enviada",
                valorNuevo: "rechazada");

            await _context.SaveChangesAsync();
        }

        private static void ValidarDestinatarioActivo(Propuesta propuesta)
        {
            if (propuesta.ProspectoId is not null && propuesta.Prospecto!.Estado != EstadoProspecto.Activo)
                throw new ReglaNegocioException("No se puede continuar: el prospecto está inactivo o descartado.");

            if (propuesta.ClienteId is not null && propuesta.Cliente!.Estado != EstadoCliente.Activo)
                throw new ReglaNegocioException("No se puede continuar: el cliente está inactivo.");
        }
    }
}
