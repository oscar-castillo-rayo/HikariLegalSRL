using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Clientes
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly IGeografiaService _geografiaService;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteService clienteService,
            IGeografiaService geografiaService,
            IPermisoEvaluador permisoEvaluador,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientesController> logger)
        {
            _clienteService = clienteService;
            _geografiaService = geografiaService;
            _permisoEvaluador = permisoEvaluador;
            _userManager = userManager;
            _logger = logger;
        }

        [Permiso(Permisos.Clientes.Ver)]
        public async Task<IActionResult> Index(string? buscar)
        {
            var viewModel = new ClienteIndexViewModel
            {
                Buscar = buscar,
                Clientes = await _clienteService.Listar(buscar)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Permiso(Permisos.Clientes.Ver)]
        public async Task<IActionResult> Detalle(int id)
        {
            var cliente = await _clienteService.ObtenerDetalle(id);
            if (cliente is null)
                return NotFound();

            ViewBag.Responsables = await ObtenerResponsablesActivos();

            return View(cliente);
        }

        [HttpGet]
        [Permiso(Permisos.Clientes.Editar)]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var viewModel = await _clienteService.ObtenerParaEditar(id);
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
        [Permiso(Permisos.Clientes.Editar)]
        public async Task<IActionResult> Editar(ClienteEditViewModel model)
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
                await _clienteService.Editar(model.Id, model.Cliente, usuarioActualId);
                TempData["Exito"] = "Cliente actualizado correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = model.Id });
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al editar cliente {ClienteId}", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Provincias = await _geografiaService.ObtenerProvincias();
                model.Paises = await _geografiaService.ObtenerPaises();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Clientes.Desactivar)]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _clienteService.Desactivar(id, usuarioActualId);
                TempData["Exito"] = "Cliente desactivado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Clientes.Reactivar)]
        public async Task<IActionResult> Reactivar(int id)
        {
            var usuarioActualId = _userManager.GetUserId(User)!;
            try
            {
                await _clienteService.Reactivar(id, usuarioActualId);
                TempData["Exito"] = "Cliente reactivado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Clientes.Asignar)]
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
                await _clienteService.ReasignarResponsable(id, responsable.ResponsableId, usuarioActualId);
                TempData["Exito"] = "Responsable reasignado correctamente.";
            }
            catch (ReglaNegocioException ex)
            {
                _logger.LogWarning(ex, "Error de regla de negocio al reasignar responsable del cliente {ClienteId}", id);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        private async Task<List<UsuarioOpcionDTO>> ObtenerResponsablesActivos()
        {
            var responsables = await _permisoEvaluador.UsuariosActivosConPermisoAsync(Permisos.Clientes.SerResponsable);
            return responsables
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new UsuarioOpcionDTO { Id = u.Id, Nombre = u.NombreCompleto })
                .ToList();
        }
    }
}
