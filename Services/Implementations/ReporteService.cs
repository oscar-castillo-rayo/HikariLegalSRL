using System.Globalization;
using HikariLegalSRL.Constants;
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

        private const int UmbralCargaMedia = 3;
        private const int UmbralCargaAlta = 6;
        private const int UmbralDemandaMedia = 8;
        private const int UmbralDemandaAlta = 20;

        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;

        public ReporteService(ApplicationDbContext context, IPermisoEvaluador permisoEvaluador)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
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

        public async Task<ReporteCargaTrabajoDTO> ObtenerCargaTrabajo()
        {
            var hoy = DateTime.Today;

            var tareasAbiertas = await _context.Tareas
                .AsNoTracking()
                .Where(t => t.Estado != EstadoTarea.Aprobada && t.Estado != EstadoTarea.ListaRevision)
                .Select(t => new
                {
                    t.ColaboradorResponsableId,
                    t.ColaboradorResponsable.NombreCompleto,
                    t.ColaboradorResponsable.Especialidad,
                    t.Estado,
                    t.FechaLimite
                })
                .ToListAsync();

            var asignables = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.Cargar);

            var colaboradores = asignables
                .Select(u => (Id: u.Id, u.NombreCompleto, u.Especialidad))
                .Concat(tareasAbiertas.Select(t => (Id: t.ColaboradorResponsableId, t.NombreCompleto, t.Especialidad)))
                .DistinctBy(c => c.Id)
                .Select(c =>
                {
                    var tareas = tareasAbiertas.Where(t => t.ColaboradorResponsableId == c.Id).ToList();
                    var total = tareas.Count;

                    return new CargaColaboradorDTO
                    {
                        ColaboradorId = c.Id,
                        NombreCompleto = c.NombreCompleto,
                        Especialidad = c.Especialidad,
                        Pendientes = tareas.Count(t => t.Estado == EstadoTarea.Pendiente),
                        EnProceso = tareas.Count(t => t.Estado is EstadoTarea.EnProceso or EstadoTarea.Devuelta),
                        Vencidas = tareas.Count(t => t.FechaLimite.Date < hoy),
                        TotalActivas = total,
                        Nivel = total >= UmbralCargaAlta ? NivelCarga.Alta
                            : total >= UmbralCargaMedia ? NivelCarga.Media
                            : NivelCarga.Baja
                    };
                })
                .OrderByDescending(c => c.TotalActivas)
                .ThenByDescending(c => c.Vencidas)
                .ThenBy(c => c.NombreCompleto)
                .ToList();

            return new ReporteCargaTrabajoDTO
            {
                UmbralCargaMedia = UmbralCargaMedia,
                UmbralCargaAlta = UmbralCargaAlta,
                Colaboradores = colaboradores
            };
        }

        public async Task<ReporteComparativoServiciosDTO> ObtenerComparativoServicios(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var usos = await _context.PropuestaServicios
                .AsNoTracking()
                .Where(ps => ps.Propuesta.Estado == EstadoPropuesta.Aceptada
                    && ps.Propuesta.FechaResolucion != null
                    && ps.Propuesta.FechaResolucion.Value.Date >= desdeFinal
                    && ps.Propuesta.FechaResolucion.Value.Date <= hastaFinal)
                .Select(ps => new { ps.ServicioId, ps.PropuestaId })
                .ToListAsync();

            var propuestasAceptadas = usos.Select(u => u.PropuestaId).Distinct().Count();

            var vecesPorServicio = usos
                .GroupBy(u => u.ServicioId)
                .ToDictionary(g => g.Key, g => g.Select(u => u.PropuestaId).Distinct().Count());

            var totalUsos = vecesPorServicio.Values.Sum();

            var catalogo = await _context.CatalogoServicios
                .AsNoTracking()
                .Select(s => new { s.ServicioId, s.Nombre, s.AreaCategoria, s.TipoServicio, s.Estado })
                .ToListAsync();

            var servicios = catalogo
                .Select(s =>
                {
                    var veces = vecesPorServicio.GetValueOrDefault(s.ServicioId);
                    var porcentaje = totalUsos == 0 ? 0 : Math.Round((decimal)veces / totalUsos * 100, 1);

                    return new DemandaServicioDTO
                    {
                        Nombre = s.Nombre,
                        AreaCategoria = s.AreaCategoria,
                        Tipo = s.TipoServicio,
                        Activo = s.Estado == EstadoServicio.Activo,
                        VecesSolicitado = veces,
                        Porcentaje = porcentaje,
                        Demanda = veces == 0 ? EstadoDemanda.SinDemanda
                            : porcentaje >= UmbralDemandaAlta ? EstadoDemanda.Alta
                            : porcentaje >= UmbralDemandaMedia ? EstadoDemanda.Media
                            : EstadoDemanda.Baja
                    };
                })
                .OrderByDescending(s => s.VecesSolicitado)
                .ThenBy(s => s.Nombre)
                .ToList();

            return new ReporteComparativoServiciosDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                ServiciosEnCatalogo = servicios.Count,
                ServiciosOfrecidos = servicios.Where(s => s.Tipo == TipoServicio.Ofrecido).Sum(s => s.VecesSolicitado),
                ServiciosSolicitados = servicios.Where(s => s.Tipo == TipoServicio.Solicitado).Sum(s => s.VecesSolicitado),
                ServiciosSinDemanda = servicios.Count(s => s.VecesSolicitado == 0),
                PropuestasAceptadas = propuestasAceptadas,
                UmbralDemandaAlta = UmbralDemandaAlta,
                UmbralDemandaMedia = UmbralDemandaMedia,
                Servicios = servicios
            };
        }

        public async Task<ReporteRentabilidadDTO> ObtenerRentabilidad(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var expedientes = await _context.Expedientes
                .AsNoTracking()
                .Where(e => e.Estado == EstadoExpediente.Cerrado
                    && e.FechaCierre != null
                    && e.FechaCierre.Value.Date >= desdeFinal
                    && e.FechaCierre.Value.Date <= hastaFinal)
                .Select(e => new
                {
                    e.ExpedienteId,
                    e.PropuestaId,
                    ClienteNombre = e.Cliente.NombreEmpresaPersona,
                    e.Propuesta.Moneda,
                    EsProBono = e.Propuesta.ModalidadPago == ModalidadPago.ProBono
                })
                .ToListAsync();

            var expedienteIds = expedientes.Select(e => e.ExpedienteId).ToList();
            var propuestaIds = expedientes.Select(e => e.PropuestaId).ToList();

            var montos = await _context.Facturas
                .AsNoTracking()
                .Where(f => expedienteIds.Contains(f.ExpedienteId) && f.Estado != EstadoFactura.Anulada)
                .Select(f => new { f.ExpedienteId, f.MontoTotal })
                .ToDictionaryAsync(f => f.ExpedienteId, f => f.MontoTotal);

            var horasEstimadas = await _context.Tareas
                .AsNoTracking()
                .Where(t => expedienteIds.Contains(t.ExpedienteId))
                .GroupBy(t => t.ExpedienteId)
                .Select(g => new { ExpedienteId = g.Key, Horas = g.Sum(t => t.HorasEstimadas) })
                .ToDictionaryAsync(g => g.ExpedienteId, g => g.Horas);

            var minutosPorRol = await _context.RegistrosHoras
                .AsNoTracking()
                .Where(r => expedienteIds.Contains(r.Tarea.ExpedienteId))
                .GroupBy(r => new { r.Tarea.ExpedienteId, r.Rol })
                .Select(g => new { g.Key.ExpedienteId, g.Key.Rol, Minutos = g.Sum(r => r.Minutos) })
                .ToListAsync();

            var minutosColaborador = minutosPorRol
                .Where(m => m.Rol == RolHoras.Colaborador)
                .ToDictionary(m => m.ExpedienteId, m => m.Minutos);

            var minutosRevisor = minutosPorRol
                .Where(m => m.Rol == RolHoras.Revisor)
                .ToDictionary(m => m.ExpedienteId, m => m.Minutos);

            var serviciosPorPropuesta = (await _context.PropuestaServicios
                .AsNoTracking()
                .Where(ps => propuestaIds.Contains(ps.PropuestaId))
                .Select(ps => new { ps.PropuestaId, ps.Precio, ps.Servicio.AreaCategoria })
                .ToListAsync())
                .GroupBy(ps => ps.PropuestaId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(ps => ps.Precio).First().AreaCategoria);

            var filas = expedientes
                .Where(e => montos.ContainsKey(e.ExpedienteId))
                .Select(e =>
                {
                    var monto = montos[e.ExpedienteId];
                    var horasEst = horasEstimadas.GetValueOrDefault(e.ExpedienteId);
                    var minutosColab = minutosColaborador.GetValueOrDefault(e.ExpedienteId);
                    var minutosRev = minutosRevisor.GetValueOrDefault(e.ExpedienteId);
                    var horasReal = Math.Round((minutosColab + minutosRev) / 60m, 2);
                    var valorHoraEst = !e.EsProBono && horasEst > 0 ? Math.Round(monto / horasEst, 2) : (decimal?)null;
                    var valorHoraReal = !e.EsProBono && horasReal > 0 ? Math.Round(monto / horasReal, 2) : (decimal?)null;
                    var eficiencia = horasEst > 0 && horasReal > 0 ? Math.Round(horasEst / horasReal * 100, 1) : (decimal?)null;

                    return new RentabilidadExpedienteDTO
                    {
                        ExpedienteId = e.ExpedienteId,
                        ClienteNombre = e.ClienteNombre,
                        AreaCategoria = serviciosPorPropuesta.GetValueOrDefault(e.PropuestaId, "Sin servicio"),
                        Moneda = e.Moneda,
                        EsProBono = e.EsProBono,
                        MontoFacturado = monto,
                        HorasEstimadas = horasEst,
                        HorasReales = horasReal,
                        HorasColaborador = minutosColab / 60m,
                        HorasRevisor = minutosRev / 60m,
                        ValorHoraEstimado = valorHoraEst,
                        ValorHoraReal = valorHoraReal,
                        Diferencia = valorHoraEst.HasValue && valorHoraReal.HasValue ? valorHoraReal - valorHoraEst : null,
                        Eficiencia = eficiencia,
                        Rentable = !e.EsProBono && eficiencia.HasValue ? eficiencia >= 100 : null
                    };
                })
                .OrderBy(f => f.AreaCategoria)
                .ThenByDescending(f => f.ExpedienteId)
                .ToList();

            var conCobro = filas.Where(f => !f.EsProBono).ToList();
            var proBono = filas.Where(f => f.EsProBono).ToList();

            var porTipoServicio = conCobro
                .GroupBy(f => new { f.AreaCategoria, f.Moneda })
                .Select(g =>
                {
                    var monto = g.Sum(f => f.MontoFacturado);
                    var horasEst = g.Sum(f => f.HorasEstimadas);
                    var horasReal = g.Sum(f => f.HorasReales);

                    return new RentabilidadTipoServicioDTO
                    {
                        AreaCategoria = g.Key.AreaCategoria,
                        Moneda = g.Key.Moneda,
                        Expedientes = g.Count(),
                        MontoFacturado = monto,
                        HorasEstimadas = horasEst,
                        HorasReales = horasReal,
                        HorasColaborador = g.Sum(f => f.HorasColaborador),
                        HorasRevisor = g.Sum(f => f.HorasRevisor),
                        ValorHoraEstimado = horasEst > 0 ? Math.Round(monto / horasEst, 2) : null,
                        ValorHoraReal = horasReal > 0 ? Math.Round(monto / horasReal, 2) : null,
                        Eficiencia = horasEst > 0 && horasReal > 0 ? Math.Round(horasEst / horasReal * 100, 1) : null
                    };
                })
                .OrderBy(g => g.AreaCategoria)
                .ThenBy(g => g.Moneda)
                .ToList();

            var conDatos = conCobro.Where(f => f.Eficiencia.HasValue).ToList();
            var horasEstGlobal = conDatos.Sum(f => f.HorasEstimadas);
            var horasRealGlobal = conDatos.Sum(f => f.HorasReales);

            return new ReporteRentabilidadDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                TotalExpedientes = filas.Count,
                Rentables = filas.Count(f => f.Rentable == true),
                NoRentables = filas.Count(f => f.Rentable == false),
                SinHoras = conCobro.Count(f => !f.Eficiencia.HasValue),
                ProBono = proBono.Count,
                HorasProBono = proBono.Sum(f => f.HorasReales),
                EficienciaGlobal = horasRealGlobal > 0 ? Math.Round(horasEstGlobal / horasRealGlobal * 100, 1) : null,
                PorTipoServicio = porTipoServicio,
                Expedientes = filas
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
