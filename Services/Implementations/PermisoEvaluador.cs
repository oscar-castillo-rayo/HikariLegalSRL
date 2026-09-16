using System.Security.Claims;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HikariLegalSRL.Services.Implementations
{
    public class PermisoEvaluador : IPermisoEvaluador
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
        private static string CacheKey(string rolId) => $"permisos:rol:{rolId}";

        public PermisoEvaluador(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<bool> TienePermisoAsync(ClaimsPrincipal user, string codigo)
        {
            var permisos = await PermisosDeUsuarioAsync(user);
            return permisos.Contains(codigo);
        }

        public async Task<bool> TienePermisoAsync(ApplicationUser usuario, string codigo)
        {
            var nombresRol = await _userManager.GetRolesAsync(usuario);

            foreach (var nombre in nombresRol)
            {
                var rol = await _roleManager.FindByNameAsync(nombre);
                if (rol is null || !rol.Activo)
                    continue;

                if (rol.EsFijo)
                    return true;

                if ((await CodigosDeRolAsync(rol)).Contains(codigo))
                    return true;
            }

            return false;
        }

        public async Task<List<ApplicationUser>> UsuariosActivosConPermisoAsync(string codigo)
        {
            var usuariosActivos = await _userManager.Users.Where(u => u.Activo).ToListAsync();
            var resultado = new List<ApplicationUser>();

            foreach (var usuario in usuariosActivos)
            {
                if (await TienePermisoAsync(usuario, codigo))
                    resultado.Add(usuario);
            }

            return resultado;
        }

        public async Task<ISet<string>> PermisosDeUsuarioAsync(ClaimsPrincipal user)
        {
            var resultado = new HashSet<string>(StringComparer.Ordinal);

            if (user.Identity?.IsAuthenticated != true)
                return resultado;

            // Rol único por usuario, pero se recorre por si en el futuro hubiera más de uno.
            var nombresRol = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct();

            foreach (var nombre in nombresRol)
            {
                var rol = await _roleManager.FindByNameAsync(nombre);
                if (rol is null || !rol.Activo)
                    continue;

                // Rol fijo (Administrador): acceso total, sin depender de que el seed haya
                // sincronizado todos los claims.
                if (rol.EsFijo)
                {
                    resultado.UnionWith(PermisosCatalogo.Codigos());
                    continue;
                }

                resultado.UnionWith(await CodigosDeRolAsync(rol));
            }

            return resultado;
        }

        public void InvalidarRol(string rolId) => _cache.Remove(CacheKey(rolId));

        private async Task<HashSet<string>> CodigosDeRolAsync(ApplicationRole rol)
        {
            return (await _cache.GetOrCreateAsync(CacheKey(rol.Id), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;

                var claims = await _roleManager.GetClaimsAsync(rol);
                return claims
                    .Where(c => c.Type == Permisos.ClaimType)
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.Ordinal);
            }))!;
        }
    }
}
