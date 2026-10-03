using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Propuestas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Propuestas
{
    [Authorize]
    public class PropuestasController : Controller
    {
        private readonly IPropuestaService _propuestaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PropuestasController> _logger;

        public PropuestasController(
            IPropuestaService propuestaService,
            UserManager<ApplicationUser> userManager,
            ILogger<PropuestasController> logger)
        {
            _propuestaService = propuestaService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Propuestas.Ver)]
        public async Task<IActionResult> Index(EstadoPropuesta? estado)
        {
            var viewModel = new PropuestaIndexViewModel
            {
                EstadoFiltro = estado,
                Propuestas = await _propuestaService.Listar(estado)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Permiso(Permisos.Propuestas.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var propuesta = await _propuestaService.ObtenerDetalle(id);
            if (propuesta is null)
                return NotFound();

            return View(propuesta);
        }

        [HttpGet]
        [Permiso(Permisos.Propuestas.Crear)]
        public async Task<IActionResult> Crear()
        {
            var viewModel = new PropuestaCreateViewModel
            {
                Prospectos = await _propuestaService.ObtenerProspectosActivos(),
                Clientes = await _propuestaService.ObtenerClientesActivos(),
                Servicios = await _propuestaService.ObtenerServiciosActivos()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Propuestas.Crear)]
        public async Task<IActionResult> Crear(PropuestaCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Prospectos = await _propuestaService.ObtenerProspectosActivos();
                model.Clientes = await _propuestaService.ObtenerClientesActivos();
                model.Servicios = await _propuestaService.ObtenerServiciosActivos();
                return View(model);
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                var propuestaId = await _propuestaService.Crear(model.Propuesta, usuarioActualId);
                TempData["Exito"] = "Propuesta registrada correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = propuestaId });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al crear propuesta");
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Prospectos = await _propuestaService.ObtenerProspectosActivos();
                model.Clientes = await _propuestaService.ObtenerClientesActivos();
                model.Servicios = await _propuestaService.ObtenerServiciosActivos();
                return View(model);
            }
        }

        [HttpGet]
        [Permiso(Permisos.Propuestas.Editar)]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var viewModel = await _propuestaService.ObtenerParaEditar(id);
                if (viewModel is null)
                    return NotFound();

                return View(viewModel);
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Detalle), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Propuestas.Editar)]
        public async Task<IActionResult> Editar(PropuestaEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Prospectos = await _propuestaService.ObtenerProspectosActivos();
                model.Clientes = await _propuestaService.ObtenerClientesActivos();
                model.Servicios = await _propuestaService.ObtenerServiciosActivos();
                return View(model);
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _propuestaService.Editar(model.Id, model.Propuesta, usuarioActualId);
                TempData["Exito"] = "Propuesta actualizada correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = model.Id });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al editar propuesta {PropuestaId}", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Prospectos = await _propuestaService.ObtenerProspectosActivos();
                model.Clientes = await _propuestaService.ObtenerClientesActivos();
                model.Servicios = await _propuestaService.ObtenerServiciosActivos();
                return View(model);
            }
        }
    }
}
