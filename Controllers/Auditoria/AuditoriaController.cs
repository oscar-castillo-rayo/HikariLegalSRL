using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Auditoria
{
    [Authorize]
    public class AuditoriaController : Controller
    {
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public AuditoriaController(IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        [HttpGet]
        [Permiso(Permisos.Auditoria.Ver)]
        public async Task<IActionResult> Index(BitacoraFiltroDTO filtro)
        {
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                TempData["Error"] = "La fecha y hora 'Desde' no pueden ser posteriores a la fecha y hora 'Hasta'. Se muestran los registros sin filtrar por fecha.";
                filtro.Desde = null;
                filtro.Hasta = null;
            }

            var resultado = await _bitacoraAuditoriaService.Consultar(filtro);
            filtro.Pagina = resultado.Pagina;

            return View(new AuditoriaIndexViewModel
            {
                Filtro = filtro,
                Resultado = resultado,
                Usuarios = await _bitacoraAuditoriaService.ObtenerUsuariosConRegistros()
            });
        }

        [HttpGet]
        [Permiso(Permisos.Auditoria.Ver)]
        public async Task<IActionResult> Detalle(long id)
        {
            var registro = await _bitacoraAuditoriaService.ObtenerDetalle(id);
            if (registro is null)
                return NotFound();

            return View(registro);
        }
    }
}
