using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50, ErrorMessage = "El apellido no puede exceder 50 caracteres")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT es obligatorio")]
        [StringLength(12, ErrorMessage = "El RUT no puede exceder 12 caracteres")]
        [Display(Name = "RUT")]
        public string RUT { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [StringLength(50, ErrorMessage = "El curso no puede exceder 50 caracteres")]
        [Display(Name = "Curso")]
        public string? Curso { get; set; }

        [Display(Name = "Préstamos Activos")]
        public int PrestamosActivos { get; set; } = 0;

        [Display(Name = "Préstamos Vencidos")]
        public int PrestamosVencidos { get; set; } = 0;

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => $"{Nombre} {Apellido}";

        [NotMapped]
        public bool EstaActivo => Estado == "Activo";

        [NotMapped]
        public bool TienePrestamosVencidos => PrestamosVencidos > 0;

        [NotMapped]
        public string EstadoBadgeClass => Estado switch
        {
            "Activo" => "badge bg-success",
            "Inactivo" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }
}