using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Clientes
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(IClienteService clienteService, ILogger<ClientesController> logger)
        {
            _clienteService = clienteService;
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

            return View(cliente);
        }
    }
}
