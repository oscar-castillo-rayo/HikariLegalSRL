using HikariLegalSRL.Constants;
using HikariLegalSRL.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HikariLegalSRL.Data
{
    public static class DbInitializer
    {
        // Punto de entrada del seed. Crea los roles base sincroniza los permisos del rol
        // Administrador con el catálogo completo y crea el usuario administrador inicial.
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var environment = services.GetRequiredService<IHostEnvironment>();

            // En desarrollo, detener el arranque si el catálogo y las constantes están desalineados.
            if (environment.IsDevelopment())
                PermisosCatalogo.Validar();

            await SeedRolesAsync(roleManager);
            await SincronizarPermisosAdminAsync(roleManager);
            await SeedAdminUserAsync(userManager);
        }

        // Crea los roles base que aún no existan. Al crear un rol NO fijo por primera vez le siembra
        // sus permisos por defecto; si el rol ya existe, no se tocan sus permisos (los administra el
        // Administrador desde la UI). Idempotente.
        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            foreach (var def in RolesBase.Todos)
            {
                if (await roleManager.RoleExistsAsync(def.Nombre))
                    continue;

                var rol = new ApplicationRole
                {
                    Name = def.Nombre,
                    Descripcion = def.Descripcion,
                    EsFijo = def.EsFijo,
                    Activo = true
                };

                var creado = await roleManager.CreateAsync(rol);
                if (!creado.Succeeded)
                    continue;

                if (RolesBase.PermisosPorDefecto.TryGetValue(def.Nombre, out var codigos))
                {
                    foreach (var codigo in codigos)
                        await roleManager.AddClaimAsync(rol, new Claim(Permisos.ClaimType, codigo));
                }
            }
        }

        // El rol Administrador (fijo) siempre queda sincronizado con el catálogo completo: se agregan
        // los códigos que falten. Nunca se quitan claims aquí.
        private static async Task SincronizarPermisosAdminAsync(RoleManager<ApplicationRole> roleManager)
        {
            var rolAdmin = await roleManager.FindByNameAsync(RolesBase.Administrador);
            if (rolAdmin is null)
                return;

            var claimsActuales = (await roleManager.GetClaimsAsync(rolAdmin))
                .Where(c => c.Type == Permisos.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permiso in PermisosCatalogo.Todas())
            {
                if (!claimsActuales.Contains(permiso.Codigo))
                    await roleManager.AddClaimAsync(rolAdmin, new Claim(Permisos.ClaimType, permiso.Codigo));
            }
        }

        // Crea el usuario administrador inicial si no existe y lo asigna al rol Administrador.
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@hikarilegal.com"; //para pruebas, se puede cambiar después desde la UI. La contraseña inicial es "Admin123!".

            if (await userManager.FindByEmailAsync(adminEmail) is not null)
                return;

            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador",
                EmailConfirmed = true,
                Especialidad = "Admin",
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, RolesBase.Administrador);
        }
    }
}
