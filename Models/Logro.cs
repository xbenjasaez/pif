using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class Logro
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(50)]
        public string Icono { get; set; } = "fa-medal"; // FontAwesome icon class

        [StringLength(20)]
        public string Color { get; set; } = "primary"; // Bootstrap color class (success, warning, info, etc.)

        // Internal code to identify the rule (e.g., "PRIMER_PRESTAMO", "LECTOR_VORAZ")
        [Required]
        [StringLength(50)]
        public string CodigoInterno { get; set; } = string.Empty;
        
        public int Puntos { get; set; } = 10;
    }
}

