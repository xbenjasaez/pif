using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(120)]
        public string? NombreCompleto { get; set; }

        public bool DebeCambiarPassword { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}

