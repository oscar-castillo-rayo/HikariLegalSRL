using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.ProBono;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.ProBono
{
    [Authorize]
    public class ProBonoController : Controller
    {
        private readonly IProBonoService _proBonoService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProBonoController> _logger;

        public ProBonoController(
            IProBonoService proBonoService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProBonoController> logger)
        {
            _proBonoService = proBonoService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Facturacion.Probono)]
        public async Task<IActionResult> Index()
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var solicitudes = await _proBonoService.Listar(usuarioActualId);
            return View(solicitudes);
        }

        [HttpGet]
        [Permiso(Permisos.Facturacion.Probono)]
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var solicitud = await _proBonoService.ObtenerDetalle(id, usuarioActualId);
            if (solicitud is null)
                return NotFound();

            return View(solicitud);
        }

        [HttpGet]
        [Permiso(Permisos.Facturacion.Probono)]
        public async Task<IActionResult> Crear()
        {
            var viewModel = new SolicitudProBonoCreateViewModel
            {
                Prospectos = await _proBonoService.ObtenerProspectosActivos(),
                Clientes = await _proBonoService.ObtenerClientesActivos()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Facturacion.Probono)]
        public async Task<IActionResult> Crear(SolicitudProBonoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Prospectos = await _proBonoService.ObtenerProspectosActivos();
                model.Clientes = await _proBonoService.ObtenerClientesActivos();
                return View(model);
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                var solicitudId = await _proBonoService.Crear(model.Solicitud, usuarioActualId);
                TempData["Exito"] = "Solicitud pro bono enviada correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = solicitudId });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al crear solicitud pro bono");
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Prospectos = await _proBonoService.ObtenerProspectosActivos();
                model.Clientes = await _proBonoService.ObtenerClientesActivos();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Facturacion.AprobarProbono)]
        public async Task<IActionResult> Resolver(int id, ResolverSolicitudProBonoDTO dto)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _proBonoService.Resolver(id, dto, usuarioActualId);
                TempData["Exito"] = "Solicitud pro bono resuelta correctamente.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al resolver solicitud pro bono {SolicitudId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
