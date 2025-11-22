using System.Collections.Generic;

namespace BibliotecaVirtualWeb.Models
{
    public class RegistroCirculacionResumenViewModel
    {
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        public int PrestamosDevueltos { get; set; }
        public int PrestamosVencidos { get; set; }
    }

    public class RegistroCirculacionViewModel
    {
        public IEnumerable<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string? EstadoSeleccionado { get; set; }
        public string? SearchString { get; set; }
        public string? RangoSeleccionado { get; set; }
        public RegistroCirculacionResumenViewModel Resumen { get; set; } = new();
    }
}

