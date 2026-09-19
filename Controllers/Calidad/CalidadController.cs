using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Calidad
{
    [Authorize]
    public class CalidadController : Controller
    {
        private readonly IEvaluacionCalidadService _evaluacionCalidadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CalidadController> _logger;

        public CalidadController(
            IEvaluacionCalidadService evaluacionCalidadService,
            UserManager<ApplicationUser> userManager,
            ILogger<CalidadController> logger)
        {
            _evaluacionCalidadService = evaluacionCalidadService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Calidad.Ver)]
        public async Task<IActionResult> Index()
        {
            var evaluaciones = await _evaluacionCalidadService.Listar();
            return View(evaluaciones);
        }

        [HttpGet]
        [Permiso(Permisos.Calidad.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var evaluacion = await _evaluacionCalidadService.ObtenerDetalle(id);
            if (evaluacion is null)
                return NotFound();

            return View(evaluacion);
        }

        [HttpGet]
        [Permiso(Permisos.Calidad.Registrar)]
        public async Task<IActionResult> Registrar(int id)
        {
            try
            {
                var expediente = await _evaluacionCalidadService.ObtenerExpedienteEvaluable(id);
                if (expediente is null)
                    return NotFound();

                return View(new EvaluacionRegistrarViewModel { Expediente = expediente });
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Expedientes");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Calidad.Registrar)]
        public async Task<IActionResult> Registrar(int id, [Bind(Prefix = "Evaluacion")] EvaluacionCalidadCreacionDTO evaluacion)
        {
            try
            {
                var expediente = await _evaluacionCalidadService.ObtenerExpedienteEvaluable(id);
                if (expediente is null)
                    return NotFound();

                if (!ModelState.IsValid)
                    return View(new EvaluacionRegistrarViewModel { Expediente = expediente, Evaluacion = evaluacion });

                var usuarioActualId = _userManager.GetUserId(User)!;
                await _evaluacionCalidadService.Registrar(id, evaluacion, usuarioActualId);

                TempData["Exito"] = "Evaluación de calidad registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al registrar evaluación de calidad del expediente {ExpedienteId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Expedientes");
            }
        }
    }
}
