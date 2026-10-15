using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Extensions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITransaccionService _transaccionService;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public UsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ITransaccionService transaccionService,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _transaccionService = transaccionService;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        [HttpGet]
        [Permiso(Permisos.Usuarios.Ver)]
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
        [Permiso(Permisos.Usuarios.Editar)]
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
        [Permiso(Permisos.Usuarios.Editar)]
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

            var nombreAnterior = usuario.NombreCompleto;
            var especialidadAnterior = usuario.Especialidad;
            var correoAnterior = usuario.Email;
            var activoAnterior = usuario.Activo;
            var rolAnterior = rolesActuales.FirstOrDefault();
            var correoCambio = usuario.Email != model.Correo;
            var rolCambio = !rolesActuales.Contains(nuevoRol.Name);

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Especialidad = model.Especialidad;
            usuario.Activo = model.Activo;

            if (correoCambio)
            {
                var usuarioExistente = await _userManager.FindByEmailAsync(model.Correo);
                if (usuarioExistente != null && usuarioExistente.Id != usuario.Id)
                {
                    ModelState.AddModelError(nameof(model.Correo), "El correo electrónico ya está en uso por otro usuario.");
                    model.RolesDisponibles = await ObtenerRolesActivosAsync();
                    return View(model);
                }
            }

            var resultadoEdicion = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                if (correoCambio)
                {
                    TransaccionFallidaException.Exigir(await _userManager.SetEmailAsync(usuario, model.Correo));
                    TransaccionFallidaException.Exigir(await _userManager.SetUserNameAsync(usuario, model.Correo));
                }

                TransaccionFallidaException.Exigir(await _userManager.UpdateAsync(usuario));

                if (rolCambio)
                {
                    TransaccionFallidaException.Exigir(await _userManager.RemoveFromRolesAsync(usuario, rolesActuales));
                    TransaccionFallidaException.Exigir(await _userManager.AddToRoleAsync(usuario, nuevoRol.Name));
                }

                // Si el usuario quedó inactivo o cambió de rol, invalidar sus sesiones activas:
                // SecurityStampValidator lo detecta en el siguiente chequeo (≤ 1 min).
                if (rolCambio || !usuario.Activo)
                    await _userManager.UpdateSecurityStampAsync(usuario);

                var (valorAnterior, valorNuevo) = ResumenDeCambios.Diferencias(
                    ("Nombre", nombreAnterior, model.NombreCompleto),
                    ("Correo", correoAnterior, model.Correo),
                    ("Especialidad", especialidadAnterior, model.Especialidad),
                    ("Rol", rolAnterior, nuevoRol.Name));

                if (valorNuevo is not null)
                {
                    await _bitacoraAuditoriaService.Registrar(
                        usuarioId: idUsuarioActual!,
                        tipoAccion: "editar",
                        moduloAfectado: "Usuarios",
                        registroAfectadoId: usuario.Id,
                        valorAnterior: valorAnterior,
                        valorNuevo: valorNuevo);
                }

                if (activoAnterior != model.Activo)
                    await RegistrarCambioEstadoAsync(idUsuarioActual!, usuario.Id, activoAnterior, model.Activo);
            });

            if (!resultadoEdicion.Succeeded)
            {
                foreach (var error in resultadoEdicion.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            TempData["Exito"] = $"Usuario {usuario.NombreCompleto} actualizado correctamente";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Permiso(Permisos.Usuarios.Crear)]
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
        [Permiso(Permisos.Usuarios.Crear)]
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

            var resultadoCreacion = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _userManager.CreateAsync(nuevoUsuario, model.Contrasena));

                var resultadoRol = await _userManager.AddToRoleAsync(nuevoUsuario, rolSeleccionado.Name);
                if (!resultadoRol.Succeeded)
                    throw new TransaccionFallidaException("No se pudo asignar el rol al usuario. Intente nuevamente.");

                await _bitacoraAuditoriaService.Registrar(
                    usuarioId: _userManager.GetUserId(User)!,
                    tipoAccion: "crear",
                    moduloAfectado: "Usuarios",
                    registroAfectadoId: nuevoUsuario.Id,
                    valorNuevo: $"{nuevoUsuario.NombreCompleto} ({nuevoUsuario.Email}), rol {rolSeleccionado.Name}");
            });

            if (!resultadoCreacion.Succeeded)
            {
                foreach (var error in resultadoCreacion.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.RolesDisponibles = await ObtenerRolesActivosAsync();
                return View(model);
            }

            TempData["Exito"] = $"Usuario {nuevoUsuario.NombreCompleto} creado correctamente";
            return RedirectToAction(nameof(Index));
        }

        private async Task RegistrarCambioEstadoAsync(string usuarioActualId, string usuarioAfectadoId, bool activoAnterior, bool activoNuevo)
        {
            await _bitacoraAuditoriaService.Registrar(
                usuarioId: usuarioActualId,
                tipoAccion: "cambiar_estado",
                moduloAfectado: "Usuarios",
                registroAfectadoId: usuarioAfectadoId,
                valorAnterior: activoAnterior ? "activo" : "inactivo",
                valorNuevo: activoNuevo ? "activo" : "inactivo");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Usuarios.Activar)]
        public async Task<IActionResult> Desactivar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var idUsuarioActual = _userManager.GetUserId(User);

            // Validacion No puede desactivarse a sí mismo
            if (usuario.Id == idUsuarioActual)
            {
                TempData["Error"] = "No puedes desactivarte a ti mismo.";
                return RedirectToAction(nameof(Index));
            }

            // Validacion No puede desactivar al último administrador activo
            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            if (rolesActuales.Contains("Administrador"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrador");
                var administradoresActivos = admins.Count(a => a.Activo && a.Id != usuario.Id);

                if (administradoresActivos == 0)
                {
                    TempData["Error"] = "No es posible desactivar al último Administrador activo del sistema.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Ejecutar la desactivación
            usuario.Activo = false;
            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _userManager.UpdateAsync(usuario));

                // Invalida las sesiones activas del usuario: SecurityStampValidator lo saca
                // en el siguiente chequeo (≤ 1 min), sin esperar a que expire la cookie.
                await _userManager.UpdateSecurityStampAsync(usuario);

                await RegistrarCambioEstadoAsync(idUsuarioActual!, usuario.Id, true, false);
            });

            if (resultado.Succeeded)
            {
                TempData["Exito"] = $"El usuario {usuario.NombreCompleto} fue desactivado correctamente.";
            }
            else
            {
                TempData["Error"] = "Ocurrió un error al intentar desactivar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Usuarios.Activar)]
        public async Task<IActionResult> Activar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            // Ejecutar la activación
            usuario.Activo = true;
            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _userManager.UpdateAsync(usuario));

                await RegistrarCambioEstadoAsync(_userManager.GetUserId(User)!, usuario.Id, false, true);
            });

            if (resultado.Succeeded)
            {
                TempData["Exito"] = $"El usuario {usuario.NombreCompleto} ha sido reactivado correctamente.";
            }
            else
            {
                TempData["Error"] = "Ocurrió un error al intentar activar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
