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
        private readonly ILogger<ProspectosController> _logger;

        public ProspectosController(
            IProspectoService prospectoService,
            IGeografiaService geografiaService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProspectosController> logger)
        {
            _prospectoService = prospectoService;
            _geografiaService = geografiaService;
            _userManager = userManager;
            _logger = logger;
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
                var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("ModelState inválido en Crear prospecto: {Errores}", string.Join(", ", errores));

                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }

            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                _logger.LogInformation("Iniciando creación de prospecto para usuario {UserId}", usuarioActualId);

                var prospectoId = await _prospectoService.Crear(model.Prospecto, usuarioActualId);

                _logger.LogInformation("Prospecto creado exitosamente. ID: {ProspectoId}", prospectoId);
                TempData["Exito"] = "Prospecto registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al crear prospecto");
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear prospecto");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Por favor intente nuevamente.");
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
