using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Models.DTOs;
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
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
                return View(new ReporteConversionPropuestasDTO
                {
                    Periodo = periodo,
                    Desde = desde.Value.Date,
                    Hasta = hasta.Value.Date
                });
            }

            var reporte = await _reporteService.ObtenerConversionPropuestas(periodo, desde, hasta);
            return View(reporte);
        }
    }
}
