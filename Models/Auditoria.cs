using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class Auditoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Accion { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Detalle { get; set; }

        [MaxLength(450)]
        public string? UsuarioId { get; set; }

        [MaxLength(150)]
        public string? UsuarioEmail { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}

