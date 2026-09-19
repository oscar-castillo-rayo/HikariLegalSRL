using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Reportes
{
    [Authorize]
    public class ReportesController : Controller
    {
        // Cada reporte tiene su propio permiso (Reportes.Conversion, Reportes.Ingresos, ...) y el
        // Index es el hub de todos ellos: no puede exigir uno solo, porque un usuario con acceso
        // a un reporte pero no a otro igual debe poder entrar y ver el que sí le corresponde.
        private static readonly string[] PermisosDeReportes =
        {
            Permisos.Reportes.Conversion,
            Permisos.Reportes.Ingresos,
            Permisos.Reportes.Geo,
            Permisos.Reportes.Carga
        };

        private readonly IReporteService _reporteService;
        private readonly IPermisoEvaluador _permisoEvaluador;

        public ReportesController(IReporteService reporteService, IPermisoEvaluador permisoEvaluador)
        {
            _reporteService = reporteService;
            _permisoEvaluador = permisoEvaluador;
        }

        public async Task<IActionResult> Index()
        {
            foreach (var permiso in PermisosDeReportes)
            {
                if (await _permisoEvaluador.TienePermisoAsync(User, permiso))
                    return View();
            }

            return Forbid();
        }

        [HttpGet]
        [Permiso(Permisos.Reportes.Conversion)]
        public async Task<IActionResult> Conversion(string periodo = "mensual", DateTime? desde = null, DateTime? hasta = null)
        {
            DescartarRangoFechasInvalido(ref desde, ref hasta);

            var reporte = await _reporteService.ObtenerConversionPropuestas(periodo, desde, hasta);
            return View(reporte);
        }

        [HttpGet]
        [Permiso(Permisos.Reportes.Conversion)]
        public async Task<IActionResult> ConversionProspectos(string periodo = "mensual", DateTime? desde = null, DateTime? hasta = null)
        {
            DescartarRangoFechasInvalido(ref desde, ref hasta);

            var reporte = await _reporteService.ObtenerConversionProspectos(periodo, desde, hasta);
            return View(reporte);
        }

        [HttpGet]
        [Permiso(Permisos.Reportes.Ingresos)]
        public async Task<IActionResult> IngresosPorServicio(string periodo = "mensual", DateTime? desde = null, DateTime? hasta = null)
        {
            DescartarRangoFechasInvalido(ref desde, ref hasta);

            var reporte = await _reporteService.ObtenerIngresosPorServicio(periodo, desde, hasta);
            return View(reporte);
        }

        [HttpGet]
        [Permiso(Permisos.Reportes.Geo)]
        public async Task<IActionResult> DistribucionGeografica(string nivel = "provincia")
        {
            var reporte = await _reporteService.ObtenerDistribucionGeografica(nivel);
            return View(reporte);
        }

        [HttpGet]
        [Permiso(Permisos.Reportes.Carga)]
        public async Task<IActionResult> CargaTrabajo()
        {
            var reporte = await _reporteService.ObtenerCargaTrabajo();
            return View(reporte);
        }

        // Un rango invertido no bloquea la pantalla: se avisa por SweetAlert (mismo patrón de
        // TempData["Error"] que usa el resto de la app) y se ignora el filtro, cayendo al rango
        // por defecto del período seleccionado en vez de mostrar la pantalla rota o vacía.
        private void DescartarRangoFechasInvalido(ref DateTime? desde, ref DateTime? hasta)
        {
            if (!desde.HasValue || !hasta.HasValue || desde.Value.Date <= hasta.Value.Date)
                return;

            TempData["Error"] = "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'. Se muestra el período por defecto.";
            desde = null;
            hasta = null;
        }
    }
}
