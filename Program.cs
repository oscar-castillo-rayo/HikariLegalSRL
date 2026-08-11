using HikariLegalSRL.Data;
using HikariLegalSRL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configuraciones de contraseña
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Configuraciones de Lockout
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // Agrega proveedores de tokens predeterminados para la recuperación de contraseña y la verificación de correo electrónico

// Configuración de la cookie de autenticación
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true; // Habilita la expiración deslizante para que la sesión se renueve con cada solicitud
    options.LoginPath = "/Account/Login";
});


WebApplication app = builder.Build();

// Seed en la base de datos para crear el usuario administrador si no existe
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    await DbInitializer.SeedAdminAsync(services);
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
