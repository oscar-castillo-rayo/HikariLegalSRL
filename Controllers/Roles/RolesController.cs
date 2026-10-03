using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Extensions;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using HikariLegalSRL.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HikariLegalSRL.Controllers.Roles
{
    public class RolesController : Controller
    {

        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IPermisoEvaluador _permisoEvaluador;
        private readonly ITransaccionService _transaccionService;
        private readonly IBitacoraAuditoriaService _bitacoraAuditoriaService;

        public RolesController(
            RoleManager<ApplicationRole> roleManager,
            IPermisoEvaluador permisoEvaluador,
            ITransaccionService transaccionService,
            IBitacoraAuditoriaService bitacoraAuditoriaService)
        {
            _roleManager = roleManager;
            _permisoEvaluador = permisoEvaluador;
            _transaccionService = transaccionService;
            _bitacoraAuditoriaService = bitacoraAuditoriaService;
        }

        private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task RegistrarAuditoriaAsync(string tipoAccion, string rolId, string? valorAnterior, string? valorNuevo)
        {
            await _bitacoraAuditoriaService.Registrar(
                usuarioId: UsuarioActualId,
                tipoAccion: tipoAccion,
                moduloAfectado: "Roles",
                registroAfectadoId: rolId,
                valorAnterior: valorAnterior,
                valorNuevo: valorNuevo);
        }

        private static string FormatearPermisos(IEnumerable<string> codigos)
        {
            var ordenados = codigos.OrderBy(c => c).ToList();
            return ordenados.Count == 0 ? "(sin permisos)" : string.Join(", ", ordenados);
        }

        [HttpGet]
        [Permiso(Permisos.Roles.Ver)]
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
                .Where(c => c.Type == Permisos.ClaimType)
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
        [Permiso(Permisos.Roles.GestionarPermisos)]
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
                .Where(c => c.Type == Permisos.ClaimType)
                .ToList();
            var codigosActuales = claimsActuales.Select(c => c.Value).ToHashSet();

            var aAgregar = seleccionados.Except(codigosActuales).ToList();
            var aQuitar = claimsActuales.Where(c => !seleccionados.Contains(c.Value)).ToList();

            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                foreach (var codigo in aAgregar)
                    TransaccionFallidaException.Exigir(await _roleManager.AddClaimAsync(rol, new Claim(Permisos.ClaimType, codigo)));

                foreach (var claim in aQuitar)
                    TransaccionFallidaException.Exigir(await _roleManager.RemoveClaimAsync(rol, claim));

                if (aAgregar.Count > 0 || aQuitar.Count > 0)
                    await RegistrarAuditoriaAsync("editar", rol.Id, FormatearPermisos(codigosActuales), FormatearPermisos(seleccionados));
            });

            // Los permisos del rol cambiaron: descartar la caché para que aplique de inmediato.
            _permisoEvaluador.InvalidarRol(rol.Id);

            if (!resultado.Succeeded)
            {
                TempData["Error"] = "No se pudieron guardar los permisos del rol. No se aplicó ningún cambio.";
                return RedirectToAction(nameof(Index), new { rolId = rol.Id });
            }

            TempData["Exito"] = $"Permisos de {rol.Name} actualizados correctamente.";
            return RedirectToAction(nameof(Index), new { rolId = rol.Id });
        }

        [HttpGet]
        [Permiso(Permisos.Roles.Crear)]
        public IActionResult Crear()
        {
            return View(new CrearRolViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Permisos.Roles.Crear)]
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

            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _roleManager.CreateAsync(nuevoRol));

                await RegistrarAuditoriaAsync("crear", nuevoRol.Id, null, $"{nuevoRol.Name}: {nuevoRol.Descripcion}");
            });

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
        [Permiso(Permisos.Roles.Editar)]
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
        [Permiso(Permisos.Roles.Editar)]
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

            var nombreAnterior = rol.Name;
            var descripcionAnterior = rol.Descripcion;
            var activoAnterior = rol.Activo;

            await _roleManager.SetRoleNameAsync(rol, model.Nombre);
            rol.Descripcion = model.Descripcion;
            rol.Activo = model.Activo;

            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _roleManager.UpdateAsync(rol));

                var (valorAnterior, valorNuevo) = ResumenDeCambios.Diferencias(
                    ("Nombre", nombreAnterior, model.Nombre),
                    ("Descripción", descripcionAnterior, model.Descripcion));

                if (valorNuevo is not null)
                    await RegistrarAuditoriaAsync("editar", rol.Id, valorAnterior, valorNuevo);

                if (activoAnterior != model.Activo)
                    await RegistrarAuditoriaAsync("cambiar_estado", rol.Id, activoAnterior ? "activo" : "inactivo", model.Activo ? "activo" : "inactivo");
            });

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
        [Permiso(Permisos.Roles.Activar)]
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

            var activoAnterior = rol.Activo;
            rol.Activo = false;
            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _roleManager.UpdateAsync(rol));

                if (activoAnterior)
                    await RegistrarAuditoriaAsync("cambiar_estado", rol.Id, "activo", "inactivo");
            });

            if (resultado.Succeeded)
            {
                _permisoEvaluador.InvalidarRol(rol.Id);
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
        [Permiso(Permisos.Roles.Activar)]
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

            var activoAnterior = rol.Activo;
            rol.Activo = true;
            var resultado = await _transaccionService.EjecutarIdentityAsync(async () =>
            {
                TransaccionFallidaException.Exigir(await _roleManager.UpdateAsync(rol));

                if (!activoAnterior)
                    await RegistrarAuditoriaAsync("cambiar_estado", rol.Id, "inactivo", "activo");
            });

            if (resultado.Succeeded)
            {
                _permisoEvaluador.InvalidarRol(rol.Id);
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
