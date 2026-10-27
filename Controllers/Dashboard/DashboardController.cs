using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Dashboard
{
    /// <summary>
    /// Presenta el dashboard de inicio con las tareas por vencer, las tareas vencidas y las notificaciones recientes de la persona usuaria.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var resumen = await _dashboardService.ObtenerResumen(usuarioActualId);
            return View(resumen);
        }
    }
}
