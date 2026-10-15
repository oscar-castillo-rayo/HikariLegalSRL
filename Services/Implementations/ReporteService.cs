using System.Globalization;
using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ReporteService : IReporteService
    {
        private const string NivelPais = "pais";
        private const string NivelProvincia = "provincia";
        private const string NivelCanton = "canton";
        private const string NivelDistrito = "distrito";
        private const string ZonaExtranjero = "Extranjero";

        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        // RF-012: tasa de conversión = propuestas Aceptadas ÷ total de propuestas Enviadas en el
        // período (Enviadas aquí es cualquier propuesta que salió de Borrador, sin importar si
        // ya fue resuelta). El período filtra por FechaEnvio, no por FechaCreacion, porque una
        // propuesta en Borrador (sin enviar) no participa todavía de ninguna tasa de conversión.
        public async Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var estados = await _context.Propuestas
                .AsNoTracking()
                .Where(p => p.FechaEnvio != null
                    && p.FechaEnvio.Value.Date >= desdeFinal
                    && p.FechaEnvio.Value.Date <= hastaFinal)
                .Select(p => p.Estado)
                .ToListAsync();

            var enviadas = estados.Count(e => e == EstadoPropuesta.Enviada);
            var aceptadas = estados.Count(e => e == EstadoPropuesta.Aceptada);
            var rechazadas = estados.Count(e => e == EstadoPropuesta.Rechazada);
            var total = estados.Count;

            return new ReporteConversionPropuestasDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                Total = total,
                Enviadas = enviadas,
                Aceptadas = aceptadas,
                Rechazadas = rechazadas,
                TasaConversion = total == 0 ? 0 : Math.Round((decimal)aceptadas / total * 100, 1)
            };
        }

        // RF-012: mismo criterio que ObtenerConversionPropuestas — el período filtra por la fecha
        // de registro del prospecto (no hay una fecha de conversión/descarte propia en el modelo),
        // y sobre ese mismo grupo se cuenta cuántos quedaron en cada estado actual. La tasa usa el
        // total de prospectos registrados en el período como base (convertidos ÷ total), tal como
        // pide la HU literalmente.
        public async Task<ReporteConversionProspectosDTO> ObtenerConversionProspectos(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var prospectos = await _context.Prospectos
                .AsNoTracking()
                .Where(p => p.FechaCreacion.Date >= desdeFinal && p.FechaCreacion.Date <= hastaFinal)
                .Select(p => new { p.FechaCreacion, p.Estado })
                .ToListAsync();

            var total = prospectos.Count;
            var convertidos = prospectos.Count(p => p.Estado == EstadoProspecto.Convertido);
            var descartados = prospectos.Count(p => p.Estado == EstadoProspecto.Descartado);
            var activos = prospectos.Count(p => p.Estado == EstadoProspecto.Activo);

            var culturaEs = CultureInfo.GetCultureInfo("es-CR");

            var detalleMensual = prospectos
                .GroupBy(p => new { p.FechaCreacion.Year, p.FechaCreacion.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var registrados = g.Count();
                    var convertidosMes = g.Count(p => p.Estado == EstadoProspecto.Convertido);

                    return new DetalleMensualProspectosDTO
                    {
                        NombreMes = CapitalizarPrimeraLetra(new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", culturaEs), culturaEs),
                        Registrados = registrados,
                        Convertidos = convertidosMes,
                        Descartados = g.Count(p => p.Estado == EstadoProspecto.Descartado),
                        Activos = g.Count(p => p.Estado == EstadoProspecto.Activo),
                        TasaConversion = registrados == 0 ? 0 : Math.Round((decimal)convertidosMes / registrados * 100, 1)
                    };
                })
                .ToList();

            return new ReporteConversionProspectosDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                Total = total,
                Convertidos = convertidos,
                Descartados = descartados,
                Activos = activos,
                TasaConversion = total == 0 ? 0 : Math.Round((decimal)convertidos / total * 100, 1),
                DetalleMensual = detalleMensual
            };
        }

        // RF-012: "ingresos facturados" = monto de facturas emitidas (no anuladas) en el período,
        // filtradas por FechaEmision. Una Factura no guarda sus servicios propios — se navega
        // Factura → Expediente → Propuesta → PropuestaServicio (ver comentario en FacturaService).
        // Como una factura puede cubrir varios servicios, el monto se reparte entre sus áreas
        // (CatalogoServicio.AreaCategoria) en proporción al precio de cada servicio dentro de la
        // propuesta — así una factura pro bono (MontoTotal = 0, ver HU-021/025) reparte cero sin
        // inflar ningún área, en vez de contar el precio nominal de la propuesta como si se hubiera
        // facturado. Los montos se agrupan también por Moneda (igual que EstadoCuentaDTO en
        // FacturaService) porque sumar colones y dólares directamente no tiene sentido.
        public async Task<ReporteIngresosServicioDTO> ObtenerIngresosPorServicio(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var facturas = await _context.Facturas
                .AsNoTracking()
                .Where(f => f.Estado != EstadoFactura.Anulada
                    && f.FechaEmision.Date >= desdeFinal
                    && f.FechaEmision.Date <= hastaFinal)
                .Include(f => f.Expediente)
                    .ThenInclude(e => e.Propuesta)
                        .ThenInclude(p => p.Servicios)
                            .ThenInclude(s => s.Servicio)
                .ToListAsync();

            var monedas = facturas
                .GroupBy(f => f.Expediente.Propuesta.Moneda)
                .Select(grupoMoneda =>
                {
                    var totalFacturado = grupoMoneda.Sum(f => f.MontoTotal);
                    var montosPorArea = new Dictionary<string, decimal>();

                    foreach (var factura in grupoMoneda)
                    {
                        var servicios = factura.Expediente.Propuesta.Servicios;
                        var totalServicios = servicios.Sum(s => s.Precio);

                        if (servicios.Count == 0 || totalServicios <= 0)
                            continue;

                        foreach (var servicio in servicios)
                        {
                            var area = servicio.Servicio.AreaCategoria;
                            var monto = factura.MontoTotal * (servicio.Precio / totalServicios);
                            montosPorArea[area] = montosPorArea.GetValueOrDefault(area) + monto;
                        }
                    }

                    return new IngresosPorMonedaDTO
                    {
                        Moneda = grupoMoneda.Key,
                        TotalFacturado = totalFacturado,
                        Desglose = montosPorArea
                            .Select(kv => new IngresoPorAreaDTO
                            {
                                AreaCategoria = kv.Key,
                                MontoFacturado = Math.Round(kv.Value, 2),
                                Porcentaje = totalFacturado == 0 ? 0 : Math.Round(kv.Value / totalFacturado * 100, 1)
                            })
                            .OrderByDescending(d => d.MontoFacturado)
                            .ToList()
                    };
                })
                .OrderBy(m => m.Moneda)
                .ToList();

            return new ReporteIngresosServicioDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                Monedas = monedas
            };
        }

        public async Task<ReporteDistribucionGeograficaDTO> ObtenerDistribucionGeografica(string nivel)
        {
            var nivelFinal = nivel is NivelPais or NivelCanton or NivelDistrito ? nivel : NivelProvincia;

            var ubicaciones = await _context.Clientes
                .AsNoTracking()
                .Select(c => new
                {
                    Pais = c.Direccion.Pais.Nombre,
                    Provincia = c.Direccion.Distrito != null ? c.Direccion.Distrito.Canton.Provincia.Nombre : null,
                    Canton = c.Direccion.Distrito != null ? c.Direccion.Distrito.Canton.Nombre : null,
                    Distrito = c.Direccion.Distrito != null ? c.Direccion.Distrito.Nombre : null
                })
                .ToListAsync();

            var total = ubicaciones.Count;

            var zonas = ubicaciones
                .GroupBy(u => nivelFinal switch
                {
                    NivelPais => (Nombre: u.Pais, Detalle: (string?)null),
                    NivelDistrito when u.Distrito is not null => (Nombre: u.Distrito, Detalle: $"{u.Canton}, {u.Provincia}"),
                    NivelCanton when u.Canton is not null => (Nombre: u.Canton, Detalle: u.Provincia),
                    NivelProvincia when u.Provincia is not null => (Nombre: u.Provincia, Detalle: (string?)null),
                    _ => (Nombre: ZonaExtranjero, Detalle: (string?)null)
                })
                .Select(g => new ZonaGeograficaDTO
                {
                    Nombre = g.Key.Nombre,
                    Detalle = g.Key.Detalle,
                    Cantidad = g.Count(),
                    Porcentaje = Math.Round((decimal)g.Count() / total * 100, 1)
                })
                .OrderByDescending(z => z.Cantidad)
                .ThenBy(z => z.Nombre)
                .ToList();

            return new ReporteDistribucionGeograficaDTO
            {
                Nivel = nivelFinal,
                TotalClientes = total,
                Zonas = zonas
            };
        }

        private static string CapitalizarPrimeraLetra(string texto, CultureInfo cultura)
        {
            return texto.Length == 0 ? texto : char.ToUpper(texto[0], cultura) + texto[1..];
        }

        // Mismo criterio de zona horaria ya usado en RevisionVencimientosBackgroundService
        // (DateTime.UtcNow.Date) para no repetir el bug de +1 día ya documentado en otras
        // pantallas que usan DateTime.Today (hora local del servidor).
        private static (DateTime Desde, DateTime Hasta) ResolverRango(string periodo, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
                return (desde.Value.Date, hasta.Value.Date);

            var hoy = DateTime.UtcNow.Date;

            return periodo switch
            {
                "semanal" => (hoy.AddDays(-6), hoy),
                "cuatrimestral" => (hoy.AddMonths(-4), hoy),
                _ => (new DateTime(hoy.Year, hoy.Month, 1), hoy)
            };
        }
    }
}
