using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class UsuarioLogro
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public int LogroId { get; set; }

        public DateTime FechaObtencion { get; set; } = DateTime.Now;

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; } = null!;

        [ForeignKey("LogroId")]
        public virtual Logro Logro { get; set; } = null!;
    }
}

