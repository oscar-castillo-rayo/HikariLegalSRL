using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Notificaciones
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly INotificacionService _notificacionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            INotificacionService notificacionService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificacionesController> logger)
        {
            _notificacionService = notificacionService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var notificaciones = await _notificacionService.ObtenerParaUsuario(usuarioActualId);
            return View(notificaciones);
        }

        [HttpGet]
        public async Task<IActionResult> Ir(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                var url = await _notificacionService.MarcarLeidaYObtenerUrl(id, usuarioActualId);
                return Redirect(url);
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al abrir la notificación {NotificacionId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            try
            {
                await _notificacionService.Eliminar(id, usuarioActualId);
                TempData["Exito"] = "Notificación eliminada.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al eliminar la notificación {NotificacionId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
