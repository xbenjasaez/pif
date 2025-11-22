using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class DevolucionRecienteViewModel
    {
        public DateTime? FechaDevolucion { get; set; }
        public string Libro { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public int DiasRetraso { get; set; }
    }

    public class DevolucionRapidaViewModel
    {
        public int DevolucionesHoy { get; set; }
        public List<DevolucionRecienteViewModel> DevolucionesRecientes { get; set; } = new();
    }

    public class PrestamosResumenViewModel
    {
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Devueltos { get; set; }
        public int Vencidos { get; set; }
    }

    public class PrestamosIndexViewModel
    {
        public IEnumerable<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
        public string? EstadoSeleccionado { get; set; }
        public string? SearchString { get; set; }
        public PrestamosResumenViewModel Resumen { get; set; } = new();
    }
}

