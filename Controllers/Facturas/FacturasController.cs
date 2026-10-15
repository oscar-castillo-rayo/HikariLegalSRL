using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Facturas
{
    [Authorize]
    public class FacturasController : Controller
    {
        private readonly IFacturaService _facturaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FacturasController(IFacturaService facturaService, UserManager<ApplicationUser> userManager)
        {
            _facturaService = facturaService;
            _userManager = userManager;
        }

        [Permiso(Permisos.Facturacion.Ver)]
        public async Task<IActionResult> Index()
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var facturas = await _facturaService.Listar(usuarioActualId);
            return View(facturas);
        }

        [HttpGet]
        [Permiso(Permisos.Facturacion.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var factura = await _facturaService.ObtenerDetalle(id, usuarioActualId);
            if (factura is null)
                return NotFound();

            return View(factura);
        }
    }
}
