using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class SistemaAlerta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Mensaje { get; set; }

        [MaxLength(20)]
        public string Tipo { get; set; } = "Error";

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public bool Resuelto { get; set; } = false;

        public DateTime? FechaResuelto { get; set; }

        [MaxLength(1000)]
        public string? DetalleTecnico { get; set; }
    }
}

