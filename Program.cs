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

// Add services to the container.
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Se ajustará después según enforceHttps
});

var environment = builder.Environment.EnvironmentName?.Trim(); // Eliminar espacios en blanco
var productionConfigPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.Production.json");
IConfiguration? effectiveConfiguration = builder.Configuration;


// Log de archivos de configuración cargados
Console.WriteLine("=== Archivos de Configuración ===");
Console.WriteLine($"Entorno detectado: {environment}");
Console.WriteLine($"Directorio de contenido: {builder.Environment.ContentRootPath}");

// Verificar si existe appsettings.Production.json
if (File.Exists(productionConfigPath))
{
    Console.WriteLine($"✓ appsettings.Production.json encontrado en: {productionConfigPath}");
}
else
{
    Console.WriteLine($"✗ appsettings.Production.json NO encontrado en: {productionConfigPath}");
}

// Verificar si existe appsettings.json
var baseConfigPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
if (File.Exists(baseConfigPath))
{
    Console.WriteLine($"✓ appsettings.json encontrado en: {baseConfigPath}");
}
else
{
    Console.WriteLine($"✗ appsettings.json NO encontrado en: {baseConfigPath}");
}


string connectionString;

Console.WriteLine($"\n=== DEBUG: Verificación de Entorno ===");
Console.WriteLine($"environment valor (original): '{builder.Environment.EnvironmentName}'");
Console.WriteLine($"environment valor (trimmed): '{environment}'");
Console.WriteLine($"environment == 'Production': {string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)}");
Console.WriteLine($"File.Exists(productionConfigPath): {File.Exists(productionConfigPath)}");
Console.WriteLine($"productionConfigPath: {productionConfigPath}");

// FORZAR desarrollo local: Si no se especifica explícitamente Production, usar Development
// Solo usar Production si el entorno está explícitamente configurado como Production
var forceSingleConfigFile = true; // Temporal: usamos solo appsettings.json para evitar discrepancias
var isExplicitlyProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

if (forceSingleConfigFile && isExplicitlyProduction)
{
    Console.WriteLine("⚠ Ignorando configuración específica de Production; usando appsettings.json únicamente.");
    isExplicitlyProduction = false;
}

if (isExplicitlyProduction)
{
    if (File.Exists(productionConfigPath))
    {
        // Leer directamente del archivo de producción
        var prodConfigBuilder = new ConfigurationBuilder()
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Production.json", optional: false)
            .Build();
        
        connectionString = prodConfigBuilder.GetConnectionString("DefaultConnection");
        effectiveConfiguration = prodConfigBuilder;
        
        Console.WriteLine("✓ Usando valores directamente de appsettings.Production.json");
        if (!string.IsNullOrEmpty(connectionString))
        {
            var preview = connectionString.Length > 50 ? connectionString.Substring(0, 50) + "..." : connectionString;
            Console.WriteLine($"  ConnectionString leída: {preview}");
        }
        else
        {
            Console.WriteLine("  ConnectionString leída: VACÍA");
        }
    }
    else
    {
        Console.WriteLine($"✗ ERROR: appsettings.Production.json NO encontrado en: {productionConfigPath}");
        Console.WriteLine("  Usando configuración base (appsettings.json)");
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    }
}
else
{
    // Usar la configuración normal (appsettings.json)
    // IMPORTANTE: Leer directamente de appsettings.json para evitar que Production.json sobrescriba
    Console.WriteLine($"  FORZANDO uso de appsettings.json (no Production.json)");
    var devConfigBuilder = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    
    connectionString = devConfigBuilder.GetConnectionString("DefaultConnection");
    effectiveConfiguration = devConfigBuilder;
    
    // Verificación adicional: asegurar que la connectionString sea la correcta
    if (!string.IsNullOrEmpty(connectionString))
    {
        if (!connectionString.Contains("Database=biblioteca_virtual"))
        {
            Console.WriteLine($"  ✗ ERROR CRÍTICO: La connectionString leída NO contiene 'biblioteca_virtual'");
            Console.WriteLine($"  ConnectionString leída: {connectionString.Substring(0, Math.Min(100, connectionString.Length))}...");
            Console.WriteLine($"  Esto indica que se está leyendo de appsettings.Production.json en lugar de appsettings.json");
        }
    }
    
    Console.WriteLine($"✓ Usando configuración de DESARROLLO (entorno: '{environment}')");
    Console.WriteLine($"  Archivo: appsettings.json (LEÍDO DIRECTAMENTE, ignorando Production.json)");
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Mostrar la cadena de conexión pero ocultar la contraseña si existe
        var displayConn = connectionString;
        if (displayConn.Contains("Pwd="))
        {
            var pwdIndex = displayConn.IndexOf("Pwd=");
            var afterPwd = displayConn.Substring(pwdIndex + 4);
            var endIndex = afterPwd.IndexOf(";");
            if (endIndex > 0)
            {
                displayConn = displayConn.Substring(0, pwdIndex + 4) + "***" + displayConn.Substring(pwdIndex + 4 + endIndex);
            }
        }
        Console.WriteLine($"  ConnectionString: {displayConn}");
        
        // Verificar que sea root@localhost
        if (connectionString.Contains("Uid=root") && connectionString.Contains("Pwd=;"))
        {
            Console.WriteLine($"  ✓ Credenciales correctas: root@localhost (sin contraseña)");
        }
        else
        {
            Console.WriteLine($"  ⚠ ADVERTENCIA: Las credenciales NO son root@localhost");
        }
    }
}

// Log de configuración de base de datos
Console.WriteLine("=== Configuración de Base de Datos ===");
Console.WriteLine($"ConnectionString configurada: {!string.IsNullOrEmpty(connectionString)}");
if (!string.IsNullOrEmpty(connectionString))
{
    // Mostrar solo los primeros caracteres por seguridad
    var preview = connectionString.Length > 50 ? connectionString.Substring(0, 50) + "..." : connectionString;
    Console.WriteLine($"ConnectionString: {preview}");
}
else
{
    Console.WriteLine("⚠ ConnectionString está vacía o no se encontró");
}

// Verificar valores específicos de cada archivo
Console.WriteLine("\n=== Valores de Configuración por Archivo ===");
var baseConfig = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();
var baseConn = baseConfig.GetConnectionString("DefaultConnection");
Console.WriteLine($"appsettings.json -> ConnectionString válida: {!string.IsNullOrEmpty(baseConn)}");

if (File.Exists(productionConfigPath))
{
    var prodConfig = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Production.json", optional: false)
        .Build();
    var prodConn = prodConfig.GetConnectionString("DefaultConnection");
    Console.WriteLine($"appsettings.Production.json -> ConnectionString válida: {!string.IsNullOrEmpty(prodConn)}");
}
Console.WriteLine("=====================================\n");

var enforceHttps = effectiveConfiguration?.GetValue<bool>("Security:EnforceHttps", true) ?? true;
Console.WriteLine($"Seguridad: EnforceHttps={(enforceHttps ? "habilitado" : "deshabilitado")} (configurable en appsettings.json)");

// Configurar política de cookies según si HTTPS está habilitado
var cookieSecurePolicy = enforceHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

// Configurar base de datos según configuración
Console.WriteLine("\n=== Evaluación de la conexión a MySQL ===");
var hasValidConnectionString = !string.IsNullOrEmpty(connectionString) && connectionString.Contains("Server=");
Console.WriteLine($"connectionString no vacía: {!string.IsNullOrEmpty(connectionString)}");
Console.WriteLine($"connectionString contiene 'Server=': {connectionString?.Contains("Server=") ?? false}");

if (!hasValidConnectionString)
{
    throw new InvalidOperationException("La connectionString de MySQL no es válida. Verifica appsettings.json.");
}

Console.WriteLine("✓ Configurando MySQL/MariaDB como base de datos");
Console.WriteLine($"  ConnectionString que se usará para DbContext:");
var mysqlConnectionString = connectionString!;
var finalPreview = mysqlConnectionString.Length > 80 ? mysqlConnectionString.Substring(0, 80) + "..." : mysqlConnectionString;
Console.WriteLine($"  {finalPreview}");

if (mysqlConnectionString.Contains("Database=biblioteca_virtual"))
{
    Console.WriteLine($"  ✓ Base de datos correcta: biblioteca_virtual");
}
else if (mysqlConnectionString.Contains("Database=escuela1_biblioteca_virtual"))
{
    Console.WriteLine($"  ✗ ERROR: Está usando la base de datos de PRODUCCIÓN: escuela1_biblioteca_virtual");
    Console.WriteLine($"  Esto NO debería pasar. Verifica la lectura de appsettings.json");
}

var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(mysqlConnectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

Console.WriteLine($"  ✓ DbContext configurado con la connectionString leída directamente");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        // Contraseñas más seguras
        options.Password.RequireDigit = true;                    //  Requiere al menos un número
        options.Password.RequireNonAlphanumeric = true;         //  Requiere al menos un carácter especial (!@#$%^&*)
        options.Password.RequireUppercase = true;               //  Requiere al menos una mayúscula
        options.Password.RequireLowercase = true;                //  Requiere al menos una minúscula
        options.Password.RequiredLength = 8;                    //  Mínimo 8 caracteres (antes era 6)
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Se ajustará después según enforceHttps
});

// Ajustar política de cookies según enforceHttps después de leer la configuración
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.ApplicationScheme, 
    options =>
    {
        options.Cookie.SecurePolicy = cookieSecurePolicy;
    });

builder.Services.PostConfigure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(
    options =>
    {
        options.Cookie.SecurePolicy = cookieSecurePolicy;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.IAuditoriaService, BibliotecaVirtualWeb.Services.AuditoriaService>();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.IAlertaSistemaService, BibliotecaVirtualWeb.Services.AlertaSistemaService>();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.ImportadorService>();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.ExportacionService>();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.BackupService>();
builder.Services.AddScoped<BibliotecaVirtualWeb.Services.IGamificationService, BibliotecaVirtualWeb.Services.GamificationService>();
builder.Services.AddSingleton<ReportesPdfRenderer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Mostrar errores detallados en desarrollo
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

if (enforceHttps)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
}
    app.UseHttpsRedirection();
}
else
{
    Console.WriteLine("⚠ Redirección a HTTPS deshabilitada (Security:EnforceHttps=false).");
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

// Middleware para prevenir caché en páginas autenticadas
app.Use(async (context, next) =>
{
    // Solo aplicar a páginas que requieren autenticación (no archivos estáticos, no catálogo público)
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var isStaticFile = path.Contains("/lib/") || path.Contains("/css/") || path.Contains("/js/") || path.Contains("/images/");
    var isPublicPage = path.Contains("/catalogopublico") || path.Contains("/account/login") || path.Contains("/account/accessdenied");
    
    if (!isStaticFile && !isPublicPage && context.User?.Identity?.IsAuthenticated == true)
    {
        // Headers para prevenir que el navegador guarde en caché páginas autenticadas
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Aplicar migraciones de base de datos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        Console.WriteLine("\n=== Verificando/Creando tablas en la base de datos ===");
        
        // Verificar conexión primero
        if (!context.Database.CanConnect())
        {
            Console.WriteLine("✗ ERROR: No se puede conectar a la base de datos");
            Console.WriteLine("   Verifica que MySQL esté corriendo y que la base de datos 'biblioteca_virtual' exista");
        }
        else
        {
            Console.WriteLine("✓ Conexión a la base de datos exitosa");
            
            // Asegurar que todas las tablas estén creadas
            Console.WriteLine("Creando/verificando tablas...");
            context.Database.EnsureCreated();
            EnsureUsuariosCursoColumn(context);
            Console.WriteLine("✓ Tablas verificadas/creadas");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ ERROR al configurar base de datos: {ex.Message}");
        Console.WriteLine($"   Tipo: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Error interno: {ex.InnerException.Message}");
        }
        Console.WriteLine($"   StackTrace: {ex.StackTrace}\n");
        System.Diagnostics.Debug.WriteLine($"Error al configurar base de datos: {ex.Message}");
    }

        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider, effectiveConfiguration ?? builder.Configuration);
}

// Configurar puertos de escucha
app.Urls.Clear();
if (enforceHttps)
{
    app.Urls.Add("https://localhost:5001");
    app.Urls.Add("http://localhost:5000");
}
else
{
app.Urls.Add("http://localhost:5000");
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
            Console.WriteLine("✓ Columna 'Curso' agregada a la tabla Usuarios.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ No se pudo asegurar la columna 'Curso' en Usuarios: {ex.Message}");
    }
}