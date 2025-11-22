using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsuariosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditoriaService _auditoria;

        public AdminUsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditoriaService auditoria)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditoria = auditoria;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var lista = new List<StaffUserListItemViewModel>();
            foreach (var user in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(user);
                lista.Add(new StaffUserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    NombreCompleto = user.NombreCompleto,
                    Roles = roles,
                    FechaRegistro = user.FechaRegistro
                });
            }

            return View(lista);
        }

        public async Task<IActionResult> Create()
        {
            var model = new CreateStaffUserViewModel
            {
                RolesDisponibles = await ObtenerRolesSelectList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffUserViewModel model)
        {
            model.RolesDisponibles = await ObtenerRolesSelectList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLowerInvariant(),
                Email = model.Email.Trim(),
                NombreCompleto = model.NombreCompleto.Trim(),
                EmailConfirmed = true // Se deja listo para verificación futura
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.RolSeleccionado))
            {
                await _userManager.AddToRoleAsync(user, model.RolSeleccionado);
            }

            await _auditoria.RegistrarAsync(
                "Crear usuario interno",
                $"Se creó el usuario {model.Email} con rol {model.RolSeleccionado}",
                User);

            TempData["SuccessMessage"] = $"Usuario {model.Email} creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var rolesUsuario = await _userManager.GetRolesAsync(user);
            var model = new EditStaffUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                NombreCompleto = user.NombreCompleto ?? string.Empty,
                RolSeleccionado = rolesUsuario.FirstOrDefault(),
                RolesDisponibles = await ObtenerRolesSelectList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditStaffUserViewModel model)
        {
            model.RolesDisponibles = await ObtenerRolesSelectList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            var nuevoCorreo = model.Email.Trim();
            if (!string.Equals(user.Email, nuevoCorreo, StringComparison.OrdinalIgnoreCase))
            {
                var correoExistente = await _userManager.FindByEmailAsync(nuevoCorreo);
                if (correoExistente != null && correoExistente.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo.");
                    return View(model);
                }

                user.Email = nuevoCorreo;
                user.UserName = nuevoCorreo.ToLowerInvariant();
                user.NormalizedEmail = nuevoCorreo.ToUpperInvariant();
                user.NormalizedUserName = user.UserName.ToUpperInvariant();
            }

            user.NombreCompleto = model.NombreCompleto.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var rolesActuales = await _userManager.GetRolesAsync(user);
            var rolDeseado = model.RolSeleccionado;

            var rolesAEliminar = rolesActuales
                .Where(r => string.IsNullOrEmpty(rolDeseado) || !string.Equals(r, rolDeseado, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rolesAEliminar.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesAEliminar);
            }

            if (!string.IsNullOrEmpty(rolDeseado) && !rolesActuales.Contains(rolDeseado))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, rolDeseado);
                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NuevaPassword);
                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(nameof(model.NuevaPassword), error.Description);
                    }
                    return View(model);
                }
            }

            await _auditoria.RegistrarAsync(
                "Editar usuario interno",
                $"Se actualizó el usuario {model.Email} (Rol: {model.RolSeleccionado ?? "Sin rol"})",
                User);

            TempData["SuccessMessage"] = $"Usuario {model.Email} actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerRolesSelectList()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            });
        }
    }
}

