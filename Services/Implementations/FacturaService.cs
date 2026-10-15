using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Facturas;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class FacturaService : IFacturaService
    {
        private static readonly HashSet<string> ExtensionesComprobantePermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png"
        };
        private const long TamanoMaximoComprobanteBytes = 10 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public FacturaService(
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

        // RF-010: el Abogado/Asesor solo consulta las facturas de su propia cartera.
        // Se usa Expediente.ResponsableId (no Cliente.ResponsableId) para que la visibilidad
        // siga a quien realmente gestiona ese caso puntual: si un expediente se reasigna
        // (HU-016) sin reasignar también al cliente, quien lo trabajó debe poder ver la
        // factura que generó, no el responsable original del cliente que ya no tiene relación
        // con ese caso. Administrador y Asistente (quienes pueden registrar abonos) ven todas.
        private async Task<bool> PuedeVerTodasAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Facturacion.Abono);
        }

        private string ObtenerCarpetaAbonos()
        {
            var rutaConfigurada = _configuration["Almacenamiento:AbonosPath"] ?? "App_Data/abonos";
            return Path.Combine(_webHostEnvironment.ContentRootPath, rutaConfigurada);
        }

        public async Task<List<FacturaListaDTO>> Listar(string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var query = _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Expediente).ThenInclude(e => e.Propuesta)
                .AsQueryable();

            if (!puedeVerTodas)
                query = query.Where(f => f.Expediente.ResponsableId == usuarioActualId);

            return await query
                .OrderByDescending(f => f.FechaEmision)
                .Select(f => new FacturaListaDTO
                {
                    Id = f.FacturaId,
                    ExpedienteId = f.ExpedienteId,
                    ClienteId = f.ClienteId,
                    ClienteNombre = f.Cliente.NombreEmpresaPersona,
                    ModalidadPago = f.ModalidadPago,
                    Moneda = f.Expediente.Propuesta.Moneda,
                    MontoTotal = f.MontoTotal,
                    MontoPagado = _context.Abonos.Where(a => a.FacturaId == f.FacturaId).Sum(a => (decimal?)a.Monto) ?? 0,
                    Estado = f.Estado,
                    FechaEmision = f.FechaEmision
                })
                .ToListAsync();
        }

        public async Task<FacturaDetalleDTO?> ObtenerDetalle(int facturaId, string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Expediente).ThenInclude(e => e.Responsable)
                .Include(f => f.Expediente).ThenInclude(e => e.Propuesta).ThenInclude(p => p.Servicios).ThenInclude(s => s.Servicio)
                .FirstOrDefaultAsync(f => f.FacturaId == facturaId);

            if (factura is null)
                return null;

            if (!puedeVerTodas && factura.Expediente.ResponsableId != usuarioActualId)
                return null;

            var abonos = await _context.Abonos
                .AsNoTracking()
                .Include(a => a.RegistradoPor)
                .Where(a => a.FacturaId == facturaId)
                .OrderBy(a => a.Fecha)
                .ToListAsync();

            var montoPagado = abonos.Sum(a => a.Monto);

            return new FacturaDetalleDTO
            {
                Id = factura.FacturaId,
                ExpedienteId = factura.ExpedienteId,
                ClienteId = factura.ClienteId,
                ClienteNombre = factura.Cliente.NombreEmpresaPersona,
                ResponsableNombre = factura.Expediente.Responsable.NombreCompleto,
                ModalidadPago = factura.ModalidadPago,
                Moneda = factura.Expediente.Propuesta.Moneda,
                MontoTotal = factura.MontoTotal,
                Estado = factura.Estado,
                FechaEmision = factura.FechaEmision,
                FechaAnulacion = factura.FechaAnulacion,
                Servicios = factura.Expediente.Propuesta.Servicios
                    .Select(s => new FacturaServicioDTO
                    {
                        NombreServicio = s.Servicio.Nombre,
                        DescripcionServicio = s.DescripcionServicio,
                        Precio = s.Precio
                    })
                    .ToList(),
                MontoPagado = montoPagado,
                SaldoPendiente = factura.MontoTotal - montoPagado,
                Abonos = abonos.Select(a => new AbonoDTO
                {
                    Id = a.AbonoId,
                    Fecha = a.Fecha,
                    Monto = a.Monto,
                    MetodoPago = a.MetodoPago,
                    NumeroComprobante = a.NumeroComprobante,
                    TieneComprobante = a.ComprobanteArchivo is not null,
                    RegistradoPorNombre = a.RegistradoPor.NombreCompleto
                }).ToList()
            };
        }

        public async Task<RegistrarAbonoViewModel?> ObtenerParaRegistrarAbono(int facturaId, string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);
            if (!puedeVerTodas)
                return null;

            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.FacturaId == facturaId);

            if (factura is null)
                return null;

            var montoPagado = await _context.Abonos
                .Where(a => a.FacturaId == facturaId)
                .SumAsync(a => (decimal?)a.Monto) ?? 0;

            return new RegistrarAbonoViewModel
            {
                FacturaId = factura.FacturaId,
                ClienteNombre = factura.Cliente.NombreEmpresaPersona,
                MontoTotal = factura.MontoTotal,
                SaldoPendiente = factura.MontoTotal - montoPagado,
                Estado = factura.Estado
            };
        }

        public async Task RegistrarAbono(int facturaId, AbonoRegistroDTO dto, string usuarioActualId)
        {
            var puedeRegistrar = await PuedeVerTodasAsync(usuarioActualId);
            if (!puedeRegistrar)
                throw new ReglaNegocioException("No tiene permiso para registrar abonos.");

            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.FacturaId == facturaId)
                ?? throw new ReglaNegocioException("La factura indicada no existe.");

            if (factura.Estado == EstadoFactura.Anulada)
                throw new ReglaNegocioException("No se pueden registrar abonos sobre una factura anulada.");

            var montoPagado = await _context.Abonos
                .Where(a => a.FacturaId == facturaId)
                .SumAsync(a => (decimal?)a.Monto) ?? 0;

            var saldoPendiente = factura.MontoTotal - montoPagado;

            if (dto.Monto > saldoPendiente)
                throw new ReglaNegocioException($"El monto del abono ({dto.Monto:N2}) supera el saldo pendiente de la factura ({saldoPendiente:N2}).");

            var comprobante = dto.Comprobante;
            if (comprobante is not null)
            {
                if (comprobante.Length > TamanoMaximoComprobanteBytes)
                    throw new ReglaNegocioException("El comprobante no puede superar los 10 MB.");

                var extension = Path.GetExtension(comprobante.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !ExtensionesComprobantePermitidas.Contains(extension))
                    throw new ReglaNegocioException("El comprobante debe ser un PDF o una imagen (jpg, jpeg, png).");
            }

            var abono = new Abono
            {
                FacturaId = facturaId,
                Fecha = dto.Fecha,
                Monto = dto.Monto,
                NumeroComprobante = string.IsNullOrWhiteSpace(dto.NumeroComprobante) ? null : dto.NumeroComprobante,
                MetodoPago = dto.MetodoPago!.Value,
                RegistradoPorId = usuarioActualId
            };

            _context.Abonos.Add(abono);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ReglaNegocioException(ex.InnerException?.Message ?? ex.Message);
            }

            if (comprobante is not null)
            {
                var nombreArchivo = Path.GetFileName(comprobante.FileName);
                var carpetaAbono = Path.Combine(ObtenerCarpetaAbonos(), abono.AbonoId.ToString());
                Directory.CreateDirectory(carpetaAbono);

                var rutaFisica = Path.Combine(carpetaAbono, nombreArchivo);
                using (var destino = File.Create(rutaFisica))
                {
                    await comprobante.CopyToAsync(destino);
                }

                abono.ComprobanteArchivo = $"{abono.AbonoId}/{nombreArchivo}";
            }

            var estadoAnterior = factura.Estado;
            // Se recalcula la suma después del insert (en vez de usar montoPagado + dto.Monto)
            // para reflejar abonos concurrentes de otro usuario sobre la misma factura.
            var nuevoMontoPagado = await _context.Abonos
                .Where(a => a.FacturaId == facturaId)
                .SumAsync(a => (decimal?)a.Monto) ?? 0;

            factura.Estado = nuevoMontoPagado >= factura.MontoTotal && factura.MontoTotal > 0
                ? EstadoFactura.Pagada
                : nuevoMontoPagado > 0
                    ? EstadoFactura.PagoParcial
                    : factura.Estado;

            await _context.SaveChangesAsync();

            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "crear",
                moduloAfectado: "Facturación",
                registroAfectadoId: facturaId.ToString(),
                valorAnterior: $"{estadoAnterior} (saldo {saldoPendiente:N2})",
                valorNuevo: $"abono {dto.Monto:N2} vía {dto.MetodoPago} -> {factura.Estado} (saldo {factura.MontoTotal - nuevoMontoPagado:N2})");

            await _context.SaveChangesAsync();
        }

        public async Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoComprobante(int abonoId, string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var abono = await _context.Abonos
                .AsNoTracking()
                .Include(a => a.Factura).ThenInclude(f => f.Expediente)
                .FirstOrDefaultAsync(a => a.AbonoId == abonoId);

            if (abono?.ComprobanteArchivo is null)
                return null;

            if (!puedeVerTodas && abono.Factura.Expediente.ResponsableId != usuarioActualId)
                return null;

            var rutaAbsoluta = Path.Combine(ObtenerCarpetaAbonos(), abono.ComprobanteArchivo.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(rutaAbsoluta))
                return null;

            var nombreArchivo = Path.GetFileName(rutaAbsoluta);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(nombreArchivo, out var contentType))
                contentType = "application/octet-stream";

            return (rutaAbsoluta, nombreArchivo, contentType);
        }

        // RF-010: el estado de cuenta agrega todas las facturas de un cliente (no una sola,
        // como en ObtenerDetalle) y es exclusivo de quien puede registrar abonos
        // (Administrador/Asistente) — el Abogado/Asesor no tiene acceso a esta vista.
        public async Task<EstadoCuentaDTO?> ObtenerEstadoCuenta(int clienteId, string usuarioActualId)
        {
            var puedeVer = await PuedeVerTodasAsync(usuarioActualId);
            if (!puedeVer)
                return null;

            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

            if (cliente is null)
                return null;

            var facturas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Expediente).ThenInclude(e => e.Propuesta)
                .Where(f => f.ClienteId == clienteId)
                .OrderByDescending(f => f.FechaEmision)
                .Select(f => new FacturaListaDTO
                {
                    Id = f.FacturaId,
                    ExpedienteId = f.ExpedienteId,
                    ClienteId = f.ClienteId,
                    ClienteNombre = cliente.NombreEmpresaPersona,
                    ModalidadPago = f.ModalidadPago,
                    Moneda = f.Expediente.Propuesta.Moneda,
                    MontoTotal = f.MontoTotal,
                    MontoPagado = _context.Abonos.Where(a => a.FacturaId == f.FacturaId).Sum(a => (decimal?)a.Monto) ?? 0,
                    Estado = f.Estado,
                    FechaEmision = f.FechaEmision
                })
                .ToListAsync();

            // Una factura anulada no cuenta hacia los totales (quedó sin efecto), aunque
            // sigue apareciendo en el historial de la lista para trazabilidad. Se agrupa por
            // moneda porque un mismo cliente puede tener propuestas en colones y en dólares:
            // sumarlas directamente daría un total sin sentido.
            var totales = facturas
                .Where(f => f.Estado != EstadoFactura.Anulada)
                .GroupBy(f => f.Moneda)
                .Select(g => new TotalPorMonedaDTO
                {
                    Moneda = g.Key,
                    TotalFacturado = g.Sum(f => f.MontoTotal),
                    TotalPagado = g.Sum(f => f.MontoPagado)
                })
                .OrderBy(t => t.Moneda)
                .ToList();

            return new EstadoCuentaDTO
            {
                ClienteId = cliente.ClienteId,
                ClienteNombre = cliente.NombreEmpresaPersona,
                Totales = totales,
                Facturas = facturas
            };
        }
    }
}
