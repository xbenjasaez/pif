using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var enforceHttps = builder.Configuration.GetValue<bool?>("Security:EnforceHttps") ?? !builder.Environment.IsDevelopment();
var cookieSecurePolicy = enforceHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

if (!connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("La cadena de conexión de MySQL no es válida. Revisa appsettings*.json.");
}

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add(new ResponseCacheAttribute
    {
        NoStore = true,
        Location = ResponseCacheLocation.None,
        Duration = 0
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".BibliotecaVirtual.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.Cookie.Name = ".BibliotecaVirtual.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IAlertaSistemaService, AlertaSistemaService>();
builder.Services.AddScoped<ImportadorService>();
builder.Services.AddScoped<ExportacionService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddSingleton<ReportesPdfRenderer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    if (enforceHttps)
    {
        app.UseHsts();
    }
}

if (enforceHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var isStaticFile = path.Contains("/lib/") || path.Contains("/css/") || path.Contains("/js/") || path.Contains("/images/");
    var isPublicPage = path.Contains("/catalogopublico") || path.Contains("/account/login") || path.Contains("/account/accessdenied");
    
    if (!isStaticFile && !isPublicPage && context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Aplicando migraciones pendientes...");
        context.Database.Migrate();
        EnsureUsuariosCursoColumn(context);
        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
        logger.LogInformation("Base de datos lista.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Error al inicializar la base de datos");
        throw;
    }
}

app.Run();

static void EnsureUsuariosCursoColumn(ApplicationDbContext context)
{
    try
    {
        using var connection = context.Database.GetDbConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'Usuarios' AND column_name = 'Curso';";

        var existe = Convert.ToInt32(command.ExecuteScalar()) > 0;
        if (!existe)
        {
            command.CommandText = "ALTER TABLE Usuarios ADD COLUMN Curso VARCHAR(50) NULL;";
            command.ExecuteNonQuery();
            Console.WriteLine(" Columna 'Curso' agregada a la tabla Usuarios.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" No se pudo asegurar la columna 'Curso' en Usuarios: {ex.Message}");
    }
}
