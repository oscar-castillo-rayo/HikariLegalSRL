using HikariLegalSRL.Constants.HikariLegalSRL.Constants;
using HikariLegalSRL.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HikariLegalSRL.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Crear el rol de administrador si no existe
            if (!await roleManager.RoleExistsAsync("Administrador"))
            {
                var adminRole = new ApplicationRole
                {
                    Name = "Administrador",
                    Descripcion = "Rol de administrador con todos los permisos",
                    EsFijo = true
                };
                await roleManager.CreateAsync(adminRole);
            }

            // Sincronizar los permisos del rol de administrador con los permisos definidos en PermisosCatalogo
            var rolAdmin = await roleManager.FindByNameAsync("Administrador");
            if (rolAdmin != null)
            {
                var claimsActuales = (await roleManager.GetClaimsAsync(rolAdmin))
                    .Where(c => c.Type == "Permiso")
                    .Select(c => c.Value)
                    .ToHashSet();

                foreach (var permiso in PermisosCatalogo.Todas())
                {
                    if (!claimsActuales.Contains(permiso.Codigo))
                    {
                        await roleManager.AddClaimAsync(rolAdmin, new Claim("Permiso", permiso.Codigo));
                    }
                }
            }

            // Crear el usuario administrador si no existe
            var adminEmail = "admin@hikarilegal.com";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
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
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }

        }
    }
}
