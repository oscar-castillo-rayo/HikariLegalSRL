using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Expedientes
{
    [Authorize]
    public class ExpedientesController : Controller
    {
        private readonly IExpedienteService _expedienteService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExpedientesController> _logger;

        public ExpedientesController(
            IExpedienteService expedienteService,
            UserManager<ApplicationUser> userManager,
            ILogger<ExpedientesController> logger)
        {
            _expedienteService = expedienteService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Expedientes.Ver)]
        public async Task<IActionResult> Index()
        {
            var viewModel = new ViewModels.Expedientes.ExpedienteIndexViewModel
            {
                Expedientes = await _expedienteService.Listar()
            };

            return View(viewModel);
        }

        [HttpGet]
        [Permiso(Permisos.Expedientes.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var viewModel = await _expedienteService.ObtenerDetalle(id);
            if (viewModel is null)
                return NotFound();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Expedientes.Crear)]
        public async Task<IActionResult> AgregarTarea(int id, TareaCreacionDTO nuevaTarea)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revise los datos de la tarea: hay campos requeridos sin completar.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _expedienteService.AgregarTarea(id, nuevaTarea, usuarioActualId);
                TempData["Exito"] = "Tarea agregada correctamente.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al agregar tarea al expediente {ExpedienteId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
