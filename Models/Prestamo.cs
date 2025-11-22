using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaVirtualWeb.Models
{
    public class Prestamo
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ejemplar")]
        public int EjemplarId { get; set; }

        [Required]
        [Display(Name = "Libro")]
        public int LibroId { get; set; }

        [Required]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [Required]
        [Display(Name = "Fecha de Préstamo")]
        public DateTime FechaPrestamo { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Fecha de Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [Display(Name = "Fecha de Devolución")]
        public DateTime? FechaDevolucion { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        // Navegación
        [ForeignKey("EjemplarId")]
        public virtual Ejemplar Ejemplar { get; set; } = null!;

        [ForeignKey("LibroId")]
        public virtual Libro Libro { get; set; } = null!;

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; } = null!;

        // Propiedades calculadas
        [NotMapped]
        public bool EstaActivo => Estado == "Activo";

        [NotMapped]
        public bool EstaVencido => DateTime.Now > FechaVencimiento && Estado == "Activo";

        [NotMapped]
        public bool EstaPorVencer
        {
            get
            {
                var diasRestantes = (FechaVencimiento - DateTime.Now).Days;
                return diasRestantes <= 3 && diasRestantes >= 0 && Estado == "Activo";
            }
        }

        [NotMapped]
        public int DiasRestantes => (FechaVencimiento - DateTime.Now).Days;

        [NotMapped]
        public string EstadoDescripcion
        {
            get
            {
                if (Estado == "Devuelto")
                    return "Devuelto";

                if (EstaVencido)
                    return "Vencido";

                if (EstaPorVencer)
                    return "Por vencer";

                return "Vigente";
            }
        }

        [NotMapped]
        public string EstadoBadgeClass
        {
            get
            {
                if (Estado == "Devuelto")
                    return "badge bg-secondary";

                if (EstaVencido)
                    return "badge bg-danger";

                if (EstaPorVencer)
                    return "badge bg-warning";

                return "badge bg-success";
            }
        }

        public Prestamo()
        {
            FechaVencimiento = DateTime.Now.AddDays(15); // 15 días por defecto
        }
    }
}