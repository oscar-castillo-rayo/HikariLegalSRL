using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Prospectos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Prospectos
{
    [Authorize]
    public class ProspectosController : Controller
    {
        private readonly IProspectoService _prospectoService;
        private readonly IActividadSeguimientoService _actividadSeguimientoService;
        private readonly IGeografiaService _geografiaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProspectosController> _logger;

        public ProspectosController(
            IProspectoService prospectoService,
            IActividadSeguimientoService actividadSeguimientoService,
            IGeografiaService geografiaService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProspectosController> logger)
        {
            _prospectoService = prospectoService;
            _actividadSeguimientoService = actividadSeguimientoService;
            _geografiaService = geografiaService;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Prospectos.Ver)]
        public async Task<IActionResult> Index(string? buscar, byte? calificacion, int? estado)
        {
            EstadoProspecto? estadoFiltro = estado is >= 0 and <= 2 ? (EstadoProspecto)estado : null;

            var viewModel = new ProspectoIndexViewModel
            {
                Buscar = buscar,
                Calificacion = calificacion,
                Estado = estado,
                Prospectos = await _prospectoService.Listar(buscar, calificacion, estadoFiltro)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Permiso(Permisos.Prospectos.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var prospecto = await _prospectoService.ObtenerDetalle(id);
            if (prospecto is null)
                return NotFound();

            var viewModel = new ProspectoDetalleViewModel
            {
                Prospecto = prospecto,
                Actividades = await _actividadSeguimientoService.Listar(id),
                UsuariosAsignables = await _userManager.Users
                    .Where(u => u.Activo)
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new UsuarioOpcionDTO { Id = u.Id, Nombre = u.NombreCompleto })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Prospectos.Editar)]
        public async Task<IActionResult> RegistrarActividad(int id, ActividadSeguimientoFormDTO actividad)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revise los datos de la actividad de seguimiento.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            try
            {
                await _actividadSeguimientoService.Registrar(id, actividad, usuarioActualId);
                TempData["Exito"] = "Actividad de seguimiento registrada.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al registrar actividad del prospecto {ProspectoId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Prospectos.Editar)]
        public async Task<IActionResult> EditarActividad(int id, int actividadId, ActividadSeguimientoFormDTO actividad)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revise los datos de la actividad de seguimiento.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            try
            {
                await _actividadSeguimientoService.Editar(actividadId, actividad, usuarioActualId);
                TempData["Exito"] = "Actividad de seguimiento actualizada.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al editar la actividad {ActividadId}", actividadId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Prospectos.Editar)]
        public async Task<IActionResult> EliminarActividad(int id, int actividadId)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _actividadSeguimientoService.Eliminar(actividadId, usuarioActualId);
                TempData["Exito"] = "Actividad de seguimiento eliminada.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al eliminar la actividad {ActividadId}", actividadId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpGet]
        [Permiso(Permisos.Prospectos.Crear)]
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
        [Permiso(Permisos.Prospectos.Crear)]
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
        [Permiso(Permisos.Prospectos.Editar)]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var viewModel = await _prospectoService.ObtenerParaEditar(id);
                if (viewModel is null)
                    return NotFound();

                viewModel.Provincias = await _geografiaService.ObtenerProvincias();
                viewModel.Paises = await _geografiaService.ObtenerPaises();
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
        [Permiso(Permisos.Prospectos.Editar)]
        public async Task<IActionResult> Editar(ProspectoEditViewModel model)
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
                await _prospectoService.Editar(model.Id, model.Prospecto, usuarioActualId);
                TempData["Exito"] = "Prospecto actualizado correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = model.Id });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al editar prospecto {ProspectoId}", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar prospecto {ProspectoId}", model.Id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Por favor intente nuevamente.");
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Prospectos.Descartar)]
        public async Task<IActionResult> Descartar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _prospectoService.Descartar(id, usuarioActualId);
                TempData["Exito"] = "Prospecto descartado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Prospectos.Reactivar)]
        public async Task<IActionResult> Reactivar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _prospectoService.Reactivar(id, usuarioActualId);
                TempData["Exito"] = "Prospecto reactivado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Detalle), new { id });
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
