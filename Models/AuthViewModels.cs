using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BibliotecaVirtualWeb.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool Recordarme { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class CreateStaffUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña temporal")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Rol")]
        public string RolSeleccionado { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> RolesDisponibles { get; set; } = Enumerable.Empty<SelectListItem>();
    }

    public class StaffUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public DateTime FechaRegistro { get; set; }
    }

    public class EditStaffUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string? RolSeleccionado { get; set; }

        [Display(Name = "Nueva contraseña")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string? NuevaPassword { get; set; }

        public IEnumerable<SelectListItem> RolesDisponibles { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}

