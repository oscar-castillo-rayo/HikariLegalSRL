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
        private readonly IReporteService _reporteService;

        public ReportesController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [Permiso(Permisos.Reportes.Conversion)]
        public IActionResult Index()
        {
            return View();
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
