using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class FiltroReportesViewModel
    {
        [Display(Name = "Periodo")]
        public string Periodo { get; set; } = "Todo";
        
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; } = DateTime.Now;
    }

    public class ReportesDetalladosViewModel
    {
        // Estadísticas generales
        public int TotalUsuarios { get; set; }
        public int TotalLibros { get; set; }
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        
        // Informes detallados
        public double TasaDevolucionPuntual { get; set; }
        public double TasaDevolucionTardia { get; set; }
        public int TotalDevolucionesPuntuales { get; set; }
        public int TotalDevolucionesTardias { get; set; }
        public int TotalDevueltos { get; set; }
        
        public int LibrosNoDevueltos { get; set; }
        public int PrestamosVencidos { get; set; }
        public double PromedioTiempoPrestamoEnDias { get; set; }
        
        public List<CategoriaPopularViewModel> CategoriasPopulares { get; set; } = new();
        public List<UsuarioTopViewModel> TopUsuarios { get; set; } = new();
        public List<LibroTopViewModel> TopLibros { get; set; } = new();
        public List<EstadisticaMensualViewModel> EstadisticasMensuales { get; set; } = new();
        
        public FiltroReportesViewModel Filtro { get; set; } = new();
    }

    public class CategoriaPopularViewModel
    {
        public string Categoria { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        public double Porcentaje { get; set; }
    }

    public class UsuarioTopViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string RUT { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        public int PrestamosDevueltos { get; set; }
        public int PrestamosVencidos { get; set; }
    }

    public class LibroTopViewModel
    {
        public int LibroId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
    }

    public class EstadisticaMensualViewModel
    {
        public string Mes { get; set; } = string.Empty;
        public int Año { get; set; }
        public int TotalPrestamos { get; set; }
        public int TotalDevoluciones { get; set; }
    }

    public class PrestamoActivoViewModel
    {
        public int Id { get; set; }
        public string LibroTitulo { get; set; } = string.Empty;
        public string LibroAutor { get; set; } = string.Empty;
        public string EjemplarCodigo { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioRUT { get; set; } = string.Empty;
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasTranscurridos { get; set; }
        public bool EstaVencido { get; set; }
    }

    public class PrestamoVencidoViewModel
    {
        public int Id { get; set; }
        public string LibroTitulo { get; set; } = string.Empty;
        public string LibroAutor { get; set; } = string.Empty;
        public string EjemplarCodigo { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioRUT { get; set; } = string.Empty;
        public string? UsuarioTelefono { get; set; }
        public string? UsuarioEmail { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasVencido { get; set; }
    }
}

