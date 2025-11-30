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
        [RegularExpression("^[0-9]*$", ErrorMessage = "El teléfono solo puede contener números")]
        public string? Telefono { get; set; }

        [StringLength(1, ErrorMessage = "El género debe ser un único carácter")]
        [RegularExpression("^[FMfm]$", ErrorMessage = "El género debe ser F (Femenino) o M (Masculino)")]
        [Display(Name = "Género")]
        public string? Genero { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tipo de Usuario")]
        public string TipoUsuario { get; set; } = "Alumno";

        [StringLength(50, ErrorMessage = "El curso no puede exceder 50 caracteres")]
        [Display(Name = "Curso")]
        public string? Curso { get; set; }

        [StringLength(1, ErrorMessage = "La letra debe ser un único carácter")]
        [RegularExpression("^[A-Fa-f]$", ErrorMessage = "La letra debe estar entre A y F")]
        [Display(Name = "Letra")]
        public string? LetraCurso { get; set; }

        [Display(Name = "Préstamos Activos")]
        public int PrestamosActivos { get; set; } = 0;

        [Display(Name = "Préstamos Vencidos")]
        public int PrestamosVencidos { get; set; } = 0;

        public virtual ICollection<UsuarioLogro> UsuarioLogros { get; set; } = new List<UsuarioLogro>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => $"{Nombre} {Apellido}";

        [NotMapped]
        public string CursoConLetra
        {
            get
            {
                if (TipoUsuario == "Profesor")
                {
                    return "Profesor";
                }

                if (string.IsNullOrWhiteSpace(Curso))
                {
                    return string.Empty;
                }

                var letra = string.IsNullOrWhiteSpace(LetraCurso)
                    ? string.Empty
                    : $" {LetraCurso.Trim().ToUpperInvariant()}";

                return $"{Curso}{letra}".Trim();
            }
        }

        [NotMapped]
        public bool EsAlumno => TipoUsuario == "Alumno";

        [NotMapped]
        public bool EsProfesor => TipoUsuario == "Profesor";

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