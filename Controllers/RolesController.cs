using HikariLegalSRL.Models;
using HikariLegalSRL.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Controllers
{
    public class RolesController : Controller
    {

        private readonly RoleManager<ApplicationRole> _roleManager;

        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RolListaViewModel
            {
                Id = r.Id,
                Nombre = r.Name,
                Descripcion = r.Descripcion,
                Activo = r.Activo,
                EsFijo = r.EsFijo
            })
            .ToListAsync();

            return View(roles);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CrearRolViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearRolViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _roleManager.RoleExistsAsync(model.Nombre))
            {
                ModelState.AddModelError(string.Empty, "Ya existe un rol con ese nombre.");
                return View(model);
            }

            var nuevoRol = new ApplicationRole
            {
                Name = model.Nombre,
                Descripcion = model.Descripcion,
                Activo = true,
                EsFijo = false
            };

            var resultado = await _roleManager.CreateAsync(nuevoRol);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["Exito"] = $"Rol {nuevoRol.Name} creado exitosamente.";
            return RedirectToAction(nameof(Index));

        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            var rol = await _roleManager.FindByIdAsync(id);
            if (rol == null) return NotFound();

            if (rol.EsFijo)
            {
                TempData["Error"] = "El Rol de Administrador no puede ser modificado.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditarRolViewModel
            {
                Id = rol.Id,
                Nombre = rol.Name,
                Descripcion = rol.Descripcion,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarRolViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var rol = await _roleManager.FindByIdAsync(model.Id);
            if (rol == null) return NotFound();

            if (rol.EsFijo)
            {
                TempData["Error"] = "El Rol de Administrador no puede ser modificado.";
                return RedirectToAction(nameof(Index));
            }

            var rolConMismoNombre = await _roleManager.FindByNameAsync(model.Nombre);
            if (rolConMismoNombre != null && rolConMismoNombre.Id != model.Id)
            {
                ModelState.AddModelError(string.Empty, "Ya existe un rol con ese nombre.");
                return View(model);
            }

            await _roleManager.SetRoleNameAsync(rol, model.Nombre);
            rol.Descripcion = model.Descripcion;

            var resultado = await _roleManager.UpdateAsync(rol);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["Exito"] = $"Rol {rol.Name} modificado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
