using HikariLegalSRL.Models;
using HikariLegalSRL.ViewModels.Usuarios;
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
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            var modelo = new List<UsuarioListaViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);

                modelo.Add(new UsuarioListaViewModel
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Correo = usuario.Email,
                    Especialidad = usuario.Especialidad,
                    Rol = roles.FirstOrDefault() ?? "Sin rol",
                    Activo = usuario.Activo,
                    Creado = usuario.FechaCreacion
                });
            }

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null) return NotFound();

            var rolesDelUsuario = await _userManager.GetRolesAsync(usuario);

            var nombreRolActual = rolesDelUsuario.FirstOrDefault();

            string rolIdActual = null;

            if (nombreRolActual != null)
            {
                var rolActual = await _roleManager.FindByNameAsync(nombreRolActual);
                rolIdActual = rolActual?.Id;
            }

            var model = new EditarUsuarioViewModel
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Email,
                Especialidad = usuario.Especialidad,
                RolId = rolIdActual,
                Activo = usuario.Activo,
                RolesDisponibles = await ObtenerRolesActivosAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            var usuario = await _userManager.FindByIdAsync(model.Id);
            if (usuario == null) return NotFound();

            var nuevoRol = await _roleManager.FindByIdAsync(model.RolId);
            if (nuevoRol == null)
            {
                ModelState.AddModelError(string.Empty, "El rol seleccionado no es válido o no existe.");
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            var rolesActuales = await _userManager.GetRolesAsync(usuario);

            // Evitar que el último admin sea desactivado O degradado de rol
            var pierdeRolAdmin = rolesActuales.Contains("Administrador") && nuevoRol.Name != "Administrador";
            var esDesactivado = rolesActuales.Contains("Administrador") && !model.Activo;

            var idUsuarioActual = _userManager.GetUserId(User);

            if (usuario.Id == idUsuarioActual && (pierdeRolAdmin || esDesactivado))
            {
                ModelState.AddModelError(string.Empty, "No puedes desactivarte ni quitarte el rol de Administrador a ti mismo.");
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            if (pierdeRolAdmin || esDesactivado)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrador");
                var administradoresActivos = admins.Count(a => a.Activo && a.Id != usuario.Id);

                if (administradoresActivos == 0)
                {
                    ModelState.AddModelError(string.Empty, "No es posible desactivar ni quitarle el rol al último Administrador activo del sistema.");
                    model.RolesDisponibles = await ObtenerRolesActivosAsync();
                    return View(model);
                }
            }

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Especialidad = model.Especialidad;
            usuario.Activo = model.Activo;

            if (usuario.Email != model.Correo)
            {
                var usuarioExistente = await _userManager.FindByEmailAsync(model.Correo);
                if (usuarioExistente != null && usuarioExistente.Id != usuario.Id)
                {
                    ModelState.AddModelError(nameof(model.Correo), "El correo electrónico ya está en uso por otro usuario.");
                    model.RolesDisponibles = await ObtenerRolesActivosAsync();
                    return View(model);
                }
                await _userManager.SetEmailAsync(usuario, model.Correo);
                await _userManager.SetUserNameAsync(usuario, model.Correo);
            }

            var ResultadoActualizacion = await _userManager.UpdateAsync(usuario);
            if (!ResultadoActualizacion.Succeeded)
            {
                foreach (var error in ResultadoActualizacion.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            if (!rolesActuales.Contains(nuevoRol.Name))
            {
                var resultadoEliminacionRoles = await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                if (!resultadoEliminacionRoles.Succeeded)
                {
                    foreach (var error in resultadoEliminacionRoles.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.RolesDisponibles = await ObtenerRolesActivosAsync();
                    return View(model);
                }
                var resultadoAsignacionRol = await _userManager.AddToRoleAsync(usuario, nuevoRol.Name);
                if (!resultadoAsignacionRol.Succeeded)
                {
                    foreach (var error in resultadoAsignacionRol.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.RolesDisponibles = await ObtenerRolesActivosAsync();
                    return View(model);
                }
            }
            TempData["Exito"] = $"Usuario {usuario.NombreCompleto} actualizado correctamente";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var model = new CrearUsuarioViewModel
            {
                RolesDisponibles = await ObtenerRolesActivosAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
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

            var usuarioExistente = await _userManager.FindByEmailAsync(model.Correo);
            if (usuarioExistente != null)
            {
                ModelState.AddModelError(nameof(model.Correo), "El correo electrónico ya está registrado.");
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
            return RedirectToAction(nameof(Index));
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
