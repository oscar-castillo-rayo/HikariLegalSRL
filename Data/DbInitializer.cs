using HikariLegalSRL.Models;
using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Data
{
    public static class DbInitializer
    {
        /**
         * Seeds the database with an admin user if it doesn't exist.
         * @param services The service provider to resolve dependencies.
         */
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManeger = services.GetRequiredService<UserManager<ApplicationUser>>();

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

            // Crear el usuario administrador si no existe
            var adminEmail = "admin@hikarilegal.com";
            if (await userManeger.FindByEmailAsync(adminEmail) is null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NombreCompleto = "Administrador",
                    EmailConfirmed = true,
                    Especialidad = "Admin"

                };

                var result = await userManeger.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManeger.AddToRoleAsync(adminUser, "Administrador");
                }
            }

        }
    }
}
