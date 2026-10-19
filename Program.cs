using HikariLegalSRL.Authorization;
using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.ModelBinding;
using HikariLegalSRL.Models;
using HikariLegalSRL.Services.Implementations;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using SweetAlert2;

var builder = WebApplication.CreateBuilder(args);

// SweetAlert para notificaciones
builder.Services.AddSweetAlert2();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Agregar un ModelBinderProvider personalizado para manejar decimales con formato invariante (punto como separador decimal)
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Autorización por permiso (Claims de tipo "Permiso" sobre el rol):
// - PermisoPolicyProvider fabrica las políticas "Permiso:<codigo>".
// - PermisoAuthorizationHandler las evalúa vía IPermisoEvaluador.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();
builder.Services.AddScoped<IPermisoEvaluador, PermisoEvaluador>();

// Configuración de servicios para Identity y correo electrónico
builder.Services.AddTransient<IEmailSender, EmailSender>();
// Bindear configuración SMTP (sección 'Smtp' en appsettings)
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

//Interfaces and Services
builder.Services.AddScoped<ITransaccionService, TransaccionService>();
builder.Services.AddScoped<IBitacoraAuditoriaService, BitacoraAuditoriaService>();
builder.Services.AddScoped<IGeografiaService, GeografiaService>();
builder.Services.AddScoped<IProspectoService, ProspectoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IActividadSeguimientoService, ActividadSeguimientoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IPropuestaService, PropuestaService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IProBonoService, ProBonoService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IEvaluacionCalidadService, EvaluacionCalidadService>();
builder.Services.AddHostedService<DbSeedBackgroundService>();
builder.Services.AddHostedService<RevisionVencimientosBackgroundService>();

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

    // Unicidad de correo
    options.User.RequireUniqueEmail = true;
})
.AddErrorDescriber<SpanishIdentityErrorDescriber>() // Describir errores en español
.AddEntityFrameworkStores<ApplicationDbContext>() // Agrega el contexto de la base de datos para Identity
.AddSignInManager<ApplicationSignInManager>() // Agrega el SignInManager personalizado
.AddDefaultTokenProviders(); // Agrega proveedores de tokens predeterminados para la recuperación de contraseña y la verificación de correo electrónico

// Cada cuánto se revalida la cookie contra la BD. Cuando se cambia la contraseña o se desactiva un usuario, 
// la cookie se invalida en el próximo request después de este intervalo.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1); //intervalo corto para reflejar cambios de estado de usuario rápidamente.
});

// Configuración de la cookie de autenticación y redirecciones por estado
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Error/403";

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});


var app = builder.Build();

// El seed (roles, permisos del Administrador y usuario administrador) corre en segundo plano
// en DbSeedBackgroundService, después de que la aplicación arrancó.
if (app.Environment.IsDevelopment())
    PermisosCatalogo.Validar();

var desfaseServidor = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
if (desfaseServidor != TimeSpan.FromHours(-6))
    app.Logger.LogWarning("La zona horaria del servidor es UTC{Desfase}, no la de Costa Rica (UTC-6): los plazos y vencimientos se calcularán con la fecha del servidor. En Azure configure WEBSITE_TIME_ZONE='Central America Standard Time'.", desfaseServidor);


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    // El valor predeterminado de HSTS es 30 días. Se utiliza para indicar a los navegadores que solo deben comunicarse con el servidor a través de HTTPS.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Error/{0}");


app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
