using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre del Proveedor")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El contacto no puede exceder 100 caracteres")]
        [Display(Name = "Persona de Contacto")]
        public string? Contacto { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = "Donación";

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [Display(Name = "Libros Proporcionados")]
        public int LibrosProporcionados { get; set; } = 0;

        // Propiedades calculadas
        [NotMapped]
        public string TipoDescripcion => Tipo switch
        {
            "Donacion" => "Donación",
            "Compra" => "Compra",
            "Prestamo" => "Préstamo",
            "Intercambio" => "Intercambio",
            _ => "Otro"
        };

        [NotMapped]
        public string TipoBadgeClass => Tipo switch
        {
            "Donacion" => "badge bg-success",
            "Compra" => "badge bg-primary",
            "Prestamo" => "badge bg-warning",
            "Intercambio" => "badge bg-info",
            _ => "badge bg-secondary"
        };
    }
}