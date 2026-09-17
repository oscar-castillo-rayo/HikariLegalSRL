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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Expedientes.Reasignar)]
        public async Task<IActionResult> ReasignarResponsable(int id, ReasignarResponsableDTO responsable)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Debe seleccionar un responsable.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            try
            {
                await _expedienteService.ReasignarResponsable(id, responsable.ResponsableId, usuarioActualId);
                TempData["Exito"] = "Responsable reasignado correctamente.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al reasignar responsable del expediente {ExpedienteId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Expedientes.Cargar)]
        public async Task<IActionResult> IniciarTarea(int expedienteId, int tareaId)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _expedienteService.IniciarTarea(tareaId, usuarioActualId);
                TempData["Exito"] = "Tarea iniciada.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al iniciar la tarea {TareaId}", tareaId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id = expedienteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Expedientes.Cargar)]
        public async Task<IActionResult> MarcarListaParaRevision(int expedienteId, int tareaId, CargarEntregableDTO entregable)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Ingrese un tiempo válido.";
                return RedirectToAction(nameof(Detalle), new { id = expedienteId });
            }

            try
            {
                await _expedienteService.MarcarListaParaRevision(tareaId, entregable, usuarioActualId);
                TempData["Exito"] = "Tarea enviada a revisión.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al enviar a revisión la tarea {TareaId}", tareaId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id = expedienteId });
        }

        [HttpGet]
        [Permiso(Permisos.Expedientes.Ver)]
        public async Task<IActionResult> DescargarEntregable(int entregableId)
        {
            var archivo = await _expedienteService.ObtenerArchivoEntregable(entregableId);
            if (archivo is null)
                return NotFound();

            return PhysicalFile(archivo.Value.RutaAbsoluta, archivo.Value.ContentType, archivo.Value.NombreArchivo);
        }
    }
}
