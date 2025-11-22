using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class Ejemplar
    {
        public int Id { get; set; }

        [Display(Name = "Libro")]
        public int LibroId { get; set; }

        [StringLength(50, ErrorMessage = "El código de barras no puede exceder 50 caracteres")]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

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

        // Navegación
        [ForeignKey("LibroId")]
        public virtual Libro? Libro { get; set; }

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
            "En Reparacion" => "badge bg-info",
            "Extraviado" => "badge bg-dark",
            "Dado de baja" => "badge bg-secondary",
            _ => "badge bg-secondary"
        };
    }
}

