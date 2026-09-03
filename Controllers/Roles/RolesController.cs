using HikariLegalSRL.Constants.HikariLegalSRL.Constants;
using HikariLegalSRL.Models;
using HikariLegalSRL.ViewModels.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HikariLegalSRL.Controllers.Roles
{
    [Authorize(Roles = "Administrador")]
    public class RolesController : Controller
    {

        private readonly RoleManager<ApplicationRole> _roleManager;

        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? rolId)
        {
            //Busca y agrega los roles del sistema en orden alfabético.
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

            var model = new GestionarPermisosViewModel { Roles = roles };

            var idSeleccionado = rolId ?? roles.FirstOrDefault()?.Id;
            if (idSeleccionado != null)
            {
                // Construye el detalle del rol seleccionado para mostrar sus permisos.
                model.RolSeleccionado = await ConstruirDetalleAsync(idSeleccionado);
            }

            return View(model);
        }

        private async Task<RolDetalleViewModel?> ConstruirDetalleAsync(string rolId)
        {
            var rol = await _roleManager.FindByIdAsync(rolId);
            if (rol == null) return null;


            var claims = await _roleManager.GetClaimsAsync(rol);
            var codigosAsignados = claims
                .Where(c => c.Type == "Permiso")
                .Select(c => c.Value)
                .ToHashSet();

            // Construye la lista de módulos y acciones, marcando cuáles están asignadas al rol.
            var modulos = PermisosCatalogo.Modulos.Select(m => new PermisoModuloViewModel
            {
                Nombre = m.Nombre,
                Acciones = m.Acciones.Select(a => new PermisoAccionViewModel
                {
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    Descripcion = a.Descripcion,
                    Asignado = codigosAsignados.Contains(a.Codigo)
                }).ToList()
            }).ToList();

            // Construye y retorna el ViewModel con los detalles del rol y sus permisos.
            return new RolDetalleViewModel
            {
                Id = rol.Id,
                Nombre = rol.Name,
                Descripcion = rol.Descripcion,
                Activo = rol.Activo,
                EsFijo = rol.EsFijo,
                Modulos = modulos
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPermisos(string rolId, List<string>? permisosSeleccionados)
        {
            var rol = await _roleManager.FindByIdAsync(rolId);
            if (rol == null) return NotFound();

            if (rol.EsFijo)
            {
                TempData["Error"] = "Los permisos de los roles fijos no pueden ser modificados.";
                return RedirectToAction(nameof(Index));
            }

            var seleccionados = (permisosSeleccionados ?? new List<string>()).ToHashSet();

            // Ignora cualquier código que no exista en el catálogo real 
            var codigosValidos = PermisosCatalogo.Todas().Select(a => a.Codigo).ToHashSet();
            seleccionados.IntersectWith(codigosValidos);

            var claimsActuales = (await _roleManager.GetClaimsAsync(rol))
                .Where(c => c.Type == "Permiso")
                .ToList();
            var codigosActuales = claimsActuales.Select(c => c.Value).ToHashSet();

            var aAgregar = seleccionados.Except(codigosActuales);
            var aQuitar = claimsActuales.Where(c => !seleccionados.Contains(c.Value));

            foreach (var codigo in aAgregar)
                await _roleManager.AddClaimAsync(rol, new Claim("Permiso", codigo));

            foreach (var claim in aQuitar)
                await _roleManager.RemoveClaimAsync(rol, claim);

            TempData["Exito"] = $"Permisos de {rol.Name} actualizados correctamente.";
            return RedirectToAction(nameof(Index), new { rolId = rol.Id });
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
            return RedirectToAction(nameof(Index), new { rolId = nuevoRol.Id });

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
                Activo = rol.Activo
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
                TempData["Error"] = "El Rol principal del sistema no puede ser modificado.";
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
            rol.Activo = model.Activo;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var rol = await _roleManager.FindByIdAsync(id);
            if (rol == null) return NotFound();

            // Regla de negocio: Proteger los roles fijos
            if (rol.EsFijo)
            {
                TempData["Error"] = "Los roles principales del sistema no pueden ser desactivados.";
                return RedirectToAction(nameof(Index));
            }

            rol.Activo = false;
            var resultado = await _roleManager.UpdateAsync(rol);

            if (resultado.Succeeded)
            {
                TempData["Exito"] = $"El rol {rol.Name} fue desactivado correctamente.";
            }
            else
            {
                TempData["Error"] = "Ocurrió un error al intentar desactivar el rol.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var rol = await _roleManager.FindByIdAsync(id);
            if (rol == null) return NotFound();

            if (rol.EsFijo)
            {
                TempData["Error"] = "No se puede alterar el estado de los roles fijos del sistema.";
                return RedirectToAction(nameof(Index));
            }

            rol.Activo = true;
            var resultado = await _roleManager.UpdateAsync(rol);

            if (resultado.Succeeded)
            {
                TempData["Exito"] = $"El rol {rol.Name} fue activado correctamente.";
            }
            else
            {
                TempData["Error"] = "Ocurrió un error al intentar activar el rol.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
