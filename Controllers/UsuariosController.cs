using HikariLegalSRL.Models;
using HikariLegalSRL.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UsuariosController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel
            {
                RolesDisponibles = await ObtenerRolesActivosAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            var rolSeleccionado = await _roleManager.FindByIdAsync(model.RolId);

            if (rolSeleccionado == null || !rolSeleccionado.Activo)
            {
                ModelState.AddModelError(nameof(model.RolId), "El rol seleccionado no es válido.");
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            var nuevoUsuario = new ApplicationUser
            {
                UserName = model.Correo,
                Email = model.Correo,
                NombreCompleto = model.NombreCompleto,
                Especialidad = model.Especialidad,
                FechaCreacion = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var resultadoCreacion = await _userManager.CreateAsync(nuevoUsuario, model.Contrasena);

            if (!resultadoCreacion.Succeeded)
            {
                foreach (var error in resultadoCreacion.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            var resultadoRol = await _userManager.AddToRoleAsync(nuevoUsuario, rolSeleccionado.Name);

            if (!resultadoRol.Succeeded)
            {
                await _userManager.DeleteAsync(nuevoUsuario);

                ModelState.AddModelError(string.Empty, "No se pudo asignar el rol al usuario. Intente nuevamente.");
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }
            TempData["Exito"] = $"Usuario {nuevoUsuario.NombreCompleto} creado correctamente";
            return RedirectToAction("Index", "Home");
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerRolesActivosAsync()
        {
            return await _roleManager.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = r.Name
                })
                .ToListAsync();
        }
    }
}
