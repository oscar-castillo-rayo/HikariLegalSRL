using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Facturas;
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
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(IFacturaService facturaService, UserManager<ApplicationUser> userManager, ILogger<FacturasController> logger)
        {
            _facturaService = facturaService;
            _userManager = userManager;
            _logger = logger;
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

        [HttpGet]
        [Permiso(Permisos.Facturacion.Abono)]
        public async Task<IActionResult> RegistrarAbono(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var viewModel = await _facturaService.ObtenerParaRegistrarAbono(id, usuarioActualId);
            if (viewModel is null)
                return NotFound();

            if (viewModel.Estado == EstadoFactura.Anulada || viewModel.Estado == EstadoFactura.Pagada)
            {
                TempData["Error"] = "No se pueden registrar abonos sobre esta factura en su estado actual.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Facturacion.Abono)]
        public async Task<IActionResult> RegistrarAbono(int id, [Bind(Prefix = "Abono")] AbonoRegistroDTO abono)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                var actual = await _facturaService.ObtenerParaRegistrarAbono(id, usuarioActualId);
                if (actual is null)
                    return NotFound();

                actual.Abono = abono;
                return View(actual);
            }

            try
            {
                await _facturaService.RegistrarAbono(id, abono, usuarioActualId);
                TempData["Exito"] = "Abono registrado correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al registrar abono para la factura {FacturaId}", id);
                ModelState.AddModelError(string.Empty, ex.Message);

                var actual = await _facturaService.ObtenerParaRegistrarAbono(id, usuarioActualId);
                if (actual is null)
                    return NotFound();

                actual.Abono = abono;
                return View(actual);
            }
        }

        [HttpGet]
        [Permiso(Permisos.Facturacion.Ver)]
        public async Task<IActionResult> DescargarComprobante(int abonoId)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            var archivo = await _facturaService.ObtenerArchivoComprobante(abonoId, usuarioActualId);
            if (archivo is null)
                return NotFound();

            return PhysicalFile(archivo.Value.RutaAbsoluta, archivo.Value.ContentType, archivo.Value.NombreArchivo);
        }
    }
}
