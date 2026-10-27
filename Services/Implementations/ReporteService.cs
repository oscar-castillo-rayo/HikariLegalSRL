using System.Globalization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Extensions;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    /// <summary>
    /// Implementa IReporteService: calcula los reportes de analítica de la firma.
    /// </summary>
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

        // Tasa = aceptadas ÷ enviadas (toda propuesta que salió de Borrador). El período filtra
        // por FechaEnvio.
        public async Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);
            var (desdeUtc, hastaUtc) = HoraCostaRica.LimitesUtc(desdeFinal, hastaFinal);

            var estados = await _context.Propuestas
                .AsNoTracking()
                .Where(p => p.FechaEnvio != null
                    && p.FechaEnvio >= desdeUtc
                    && p.FechaEnvio < hastaUtc)
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

        // El período filtra por la fecha de registro del prospecto. Tasa = convertidos ÷ total
        // registrados en el período.
        public async Task<ReporteConversionProspectosDTO> ObtenerConversionProspectos(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);
            var (desdeUtc, hastaUtc) = HoraCostaRica.LimitesUtc(desdeFinal, hastaFinal);

            var prospectos = (await _context.Prospectos
                .AsNoTracking()
                .Where(p => p.FechaCreacion >= desdeUtc && p.FechaCreacion < hastaUtc)
                .Select(p => new { p.FechaCreacion, p.Estado })
                .ToListAsync())
                .Select(p => new { FechaCreacion = p.FechaCreacion.ALocal(), p.Estado })
                .ToList();

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

        // Suma las facturas emitidas (no anuladas) del período por FechaEmision. El monto se
        // reparte entre las áreas de los servicios de la propuesta en proporción a su precio,
        // y se agrupa por Moneda.
        public async Task<ReporteIngresosServicioDTO> ObtenerIngresosPorServicio(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);
            var (desdeUtc, hastaUtc) = HoraCostaRica.LimitesUtc(desdeFinal, hastaFinal);

            var facturas = await _context.Facturas
                .AsNoTracking()
                .Where(f => f.Estado != EstadoFactura.Anulada
                    && f.FechaEmision >= desdeUtc
                    && f.FechaEmision < hastaUtc)
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
            var hoy = HoraCostaRica.Hoy;

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

            var asignables = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Expedientes.RecibirTareas);

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
            var (desdeUtc, hastaUtc) = HoraCostaRica.LimitesUtc(desdeFinal, hastaFinal);

            var usos = await _context.PropuestaServicios
                .AsNoTracking()
                .Where(ps => ps.Propuesta.FechaEnvio != null
                    && ps.Propuesta.FechaEnvio >= desdeUtc
                    && ps.Propuesta.FechaEnvio < hastaUtc)
                .Select(ps => new { ps.ServicioId, ps.PropuestaId, ps.Propuesta.Estado })
                .ToListAsync();

            var propuestasOfrecidas = await _context.Propuestas
                .AsNoTracking()
                .CountAsync(p => p.FechaEnvio != null
                    && p.FechaEnvio >= desdeUtc
                    && p.FechaEnvio < hastaUtc);

            var propuestasAceptadas = await _context.Propuestas
                .AsNoTracking()
                .CountAsync(p => p.Estado == EstadoPropuesta.Aceptada
                    && p.FechaEnvio != null
                    && p.FechaEnvio >= desdeUtc
                    && p.FechaEnvio < hastaUtc);

            var propuestasRechazadas = await _context.Propuestas
                .AsNoTracking()
                .CountAsync(p => p.Estado == EstadoPropuesta.Rechazada
                    && p.FechaEnvio != null
                    && p.FechaEnvio >= desdeUtc
                    && p.FechaEnvio < hastaUtc);

            var ofrecidoPorServicio = usos
                .GroupBy(u => u.ServicioId)
                .ToDictionary(g => g.Key, g => g.Select(u => u.PropuestaId).Distinct().Count());

            var aceptadoPorServicio = usos
                .Where(u => u.Estado == EstadoPropuesta.Aceptada)
                .GroupBy(u => u.ServicioId)
                .ToDictionary(g => g.Key, g => g.Select(u => u.PropuestaId).Distinct().Count());

            var totalOfrecido = ofrecidoPorServicio.Values.Sum();

            var catalogo = await _context.CatalogoServicios
                .AsNoTracking()
                .Select(s => new { s.ServicioId, s.Nombre, s.AreaCategoria, s.TipoServicio, s.Estado })
                .ToListAsync();

            var servicios = catalogo
                .Select(s =>
                {
                    var ofrecido = ofrecidoPorServicio.GetValueOrDefault(s.ServicioId);
                    var aceptado = aceptadoPorServicio.GetValueOrDefault(s.ServicioId);
                    var participacion = totalOfrecido == 0 ? 0 : Math.Round((decimal)ofrecido / totalOfrecido * 100, 1);
                    var aceptacion = ofrecido == 0 ? 0 : Math.Round((decimal)aceptado / ofrecido * 100, 1);

                    return new DemandaServicioDTO
                    {
                        Nombre = s.Nombre,
                        AreaCategoria = s.AreaCategoria,
                        Tipo = s.TipoServicio,
                        Activo = s.Estado == EstadoServicio.Activo,
                        VecesOfrecido = ofrecido,
                        VecesAceptado = aceptado,
                        Participacion = participacion,
                        PorcentajeAceptacion = aceptacion,
                        Demanda = ofrecido == 0 ? EstadoDemanda.SinDemanda
                            : participacion >= UmbralDemandaAlta ? EstadoDemanda.Alta
                            : participacion >= UmbralDemandaMedia ? EstadoDemanda.Media
                            : EstadoDemanda.Baja
                    };
                })
                .OrderByDescending(s => s.VecesOfrecido)
                .ThenBy(s => s.Nombre)
                .ToList();

            return new ReporteComparativoServiciosDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                ServiciosEnCatalogo = servicios.Count(s => s.Activo),
                PropuestasOfrecidas = propuestasOfrecidas,
                ServiciosSolicitados = totalOfrecido,
                ServiciosSinDemanda = servicios.Count(s => s.Activo && s.VecesOfrecido == 0),
                PropuestasAceptadas = propuestasAceptadas,
                PropuestasRechazadas = propuestasRechazadas,
                UmbralDemandaAlta = UmbralDemandaAlta,
                UmbralDemandaMedia = UmbralDemandaMedia,
                Servicios = servicios
            };
        }

        public async Task<ReporteRentabilidadDTO> ObtenerRentabilidad(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);
            var (desdeUtc, hastaUtc) = HoraCostaRica.LimitesUtc(desdeFinal, hastaFinal);

            var expedientes = await _context.Expedientes
                .AsNoTracking()
                .Where(e => e.Estado == EstadoExpediente.Cerrado
                    && e.FechaCierre != null
                    && e.FechaCierre >= desdeUtc
                    && e.FechaCierre < hastaUtc)
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

        // Las fechas de calendario se resuelven contra HoraCostaRica.Hoy (la fecha de Costa Rica,
        // no la del servidor), igual que las pantallas, para no repetir el bug de +1 día.
        private static (DateTime Desde, DateTime Hasta) ResolverRango(string periodo, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
                return (desde.Value.Date, hasta.Value.Date);

            var hoy = HoraCostaRica.Hoy;

            return periodo switch
            {
                "semanal" => (hoy.AddDays(-6), hoy),
                "cuatrimestral" => (hoy.AddMonths(-4), hoy),
                _ => (new DateTime(hoy.Year, hoy.Month, 1), hoy)
            };
        }
    }
}
