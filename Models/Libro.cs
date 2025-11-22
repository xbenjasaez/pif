using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class Libro : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El autor es obligatorio")]
        [StringLength(100, ErrorMessage = "El autor no puede exceder 100 caracteres")]
        [Display(Name = "Autor")]
        public string Autor { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El ISBN no puede exceder 20 caracteres")]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

        [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
        [Display(Name = "Categoría")]
        public string? Categoria { get; set; }

        [Display(Name = "Año de Publicación")]
        [Column("Ano")]
        public int? Año { get; set; }

        [StringLength(100, ErrorMessage = "La editorial no puede exceder 100 caracteres")]
        [Display(Name = "Editorial")]
        public string? Editorial { get; set; }

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [StringLength(100, ErrorMessage = "La ubicación no puede exceder 100 caracteres")]
        [Display(Name = "Ubicación Física")]
        public string? Ubicacion { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Disponible";

        [Display(Name = "Fecha de Agregado")]
        public DateTime FechaAgregado { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Préstamo")]
        public DateTime? FechaPrestamo { get; set; }

        [StringLength(100, ErrorMessage = "El nombre del prestatario no puede exceder 100 caracteres")]
        [Display(Name = "Prestado a")]
        public string? PrestadoA { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [StringLength(20, ErrorMessage = "El código de barras no puede exceder 20 caracteres")]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }

        // Navegación
        [ForeignKey("ProveedorId")]
        public virtual Proveedor? Proveedor { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public bool EstaDisponible => Estado == "Disponible";

        [NotMapped]
        public bool EstaPrestado => Estado == "Prestado";

        [NotMapped]
        public bool EstaReservado => Estado == "Reservado";

        [NotMapped]
        public string EstadoBadgeClass => Estado switch
        {
            "Disponible" => "badge bg-success",
            "Prestado" => "badge bg-danger",
            "Reservado" => "badge bg-warning",
            _ => "badge bg-secondary"
        };

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Año.HasValue)
            {
                var currentYear = DateTime.Now.Year;
                if (Año > currentYear)
                {
                    yield return new ValidationResult(
                        "El año de publicación no puede ser mayor al año actual.",
                        new[] { nameof(Año) });
                }

                if (Año < 1500)
                {
                    yield return new ValidationResult(
                        "El año de publicación parece inválido. Ingrese un año posterior a 1500.",
                        new[] { nameof(Año) });
                }
            }
        }
    }
}