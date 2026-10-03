using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HikariLegalSRL.Models;

namespace HikariLegalSRL.Controllers.Servicios
{
    [Authorize]
    public class ServiciosController : Controller
    {
        private readonly IServicioService _servicioService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ServiciosController> _logger;

        public ServiciosController(
            IServicioService servicioService,
            UserManager<ApplicationUser> userManager,
            ILogger<ServiciosController> logger)
        {
            _servicioService = servicioService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Servicios.Ver)]
        public async Task<IActionResult> Index(string? buscar)
        {
            var viewModel = new ServicioIndexViewModel
            {
                Buscar = buscar,
                Servicios = await _servicioService.Listar(buscar)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Permiso(Permisos.Servicios.Crear)]
        public IActionResult Crear()
        {
            return View(new ServicioCreacionDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Servicios.Crear)]
        public async Task<IActionResult> Crear(ServicioCreacionDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _servicioService.Crear(model, usuarioActualId);
                TempData["Exito"] = "Servicio registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al crear servicio");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [Permiso(Permisos.Servicios.Editar)]
        public async Task<IActionResult> Editar(int id)
        {
            var viewModel = await _servicioService.ObtenerParaEditar(id);
            if (viewModel is null)
                return NotFound();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Servicios.Editar)]
        public async Task<IActionResult> Editar(ServicioEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _servicioService.Editar(model.Id, model.Servicio, usuarioActualId);
                TempData["Exito"] = "Servicio actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al editar servicio {ServicioId}", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Servicios.Desactivar)]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _servicioService.Desactivar(id, usuarioActualId);
                TempData["Exito"] = "Servicio desactivado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Servicios.Reactivar)]
        public async Task<IActionResult> Reactivar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _servicioService.Reactivar(id, usuarioActualId);
                TempData["Exito"] = "Servicio reactivado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
