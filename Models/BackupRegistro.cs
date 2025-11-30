using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    [Table("backup_registros")]
    public class BackupRegistro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string RutaCompleta { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("TamanoBytes")]
        public long TamañoBytes { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public bool Exitoso { get; set; } = true;
    }
}

