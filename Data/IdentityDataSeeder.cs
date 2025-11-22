using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaVirtualWeb.Data
{
    public static class IdentityDataSeeder
    {
        private static readonly string[] RolesBase = new[]
        {
            "Admin",
            "Bibliotecario",
            "Asistente"
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in RolesBase)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var superUserEmail = configuration["SuperUser:Email"] ?? "admin@biblioteca.local";
            var superUserPassword = configuration["SuperUser:Password"] ?? "Admin123!";
            var superUserName = configuration["SuperUser:FullName"] ?? "Administrador General";

            var superUser = await userManager.FindByEmailAsync(superUserEmail);
            if (superUser == null)
            {
                superUser = new ApplicationUser
                {
                    UserName = superUserEmail.ToLowerInvariant(),
                    Email = superUserEmail,
                    NombreCompleto = superUserName,
                    EmailConfirmed = true,
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superUser, superUserPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"No se pudo crear el superusuario inicial: {errors}");
                }
            }
            else
            {
                var normalizedEmail = superUserEmail.ToLowerInvariant();
                var requiresUpdate = false;

                if (!string.Equals(superUser.UserName, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    superUser.UserName = normalizedEmail;
                    requiresUpdate = true;
                }

                if (!string.Equals(superUser.Email, superUserEmail, StringComparison.OrdinalIgnoreCase))
                {
                    superUser.Email = superUserEmail;
                    superUser.NormalizedEmail = superUserEmail.ToUpperInvariant();
                    requiresUpdate = true;
                }

                if (!string.Equals(superUser.NombreCompleto, superUserName, StringComparison.Ordinal))
                {
                    superUser.NombreCompleto = superUserName;
                    requiresUpdate = true;
                }

                if (requiresUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(superUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"No se pudo actualizar el superusuario: {errors}");
                    }
                }

                var hasPassword = await userManager.CheckPasswordAsync(superUser, superUserPassword);
                if (!hasPassword)
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(superUser);
                    var resetResult = await userManager.ResetPasswordAsync(superUser, resetToken, superUserPassword);
                    if (!resetResult.Succeeded)
                    {
                        var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"No se pudo restablecer la contraseña del superusuario: {errors}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(superUser, "Admin"))
            {
                await userManager.AddToRoleAsync(superUser, "Admin");
            }
        }
    }
}

