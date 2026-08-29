using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Prospectos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Prospectos
{
    [Authorize]
    public class ProspectosController : Controller
    {
        private readonly IProspectoService _prospectoService;
        private readonly IGeografiaService _geografiaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProspectosController(IProspectoService prospectoService, IGeografiaService geografiaService, UserManager<ApplicationUser> userManager)
        {
            _prospectoService = prospectoService;
            _geografiaService = geografiaService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var viewModel = new ProspectoCreateViewModel
            {
                Provincias = await _geografiaService.ObtenerProvincias(),
                Paises = await _geografiaService.ObtenerPaises()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ProspectoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _prospectoService.Crear(model.Prospecto, usuarioActualId);
                TempData["Exito"] = "Prospecto registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerCantones(int provinciaId)
        {
            var cantones = await _geografiaService.ObtenerCantones(provinciaId);
            return Json(cantones);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDistritos(int cantonId)
        {
            var distritos = await _geografiaService.ObtenerDistritos(cantonId);
            return Json(distritos);
        }


    }
}
