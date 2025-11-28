using System.ComponentModel.DataAnnotations;

namespace BibliotecaVirtualWeb.Models
{
    public class FiltroReportesViewModel
    {
        [Display(Name = "Periodo")]
        public string Periodo { get; set; } = "Todo";
        
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; } = DateTime.Now;
        public string? CursoSeleccionado { get; set; }
        public string? CategoriaSeleccionada { get; set; }
        public IEnumerable<string> CursosDisponibles { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> CategoriasDisponibles { get; set; } = Enumerable.Empty<string>();
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
        public List<string> AlertasRapidas { get; set; } = new();
        
        public FiltroReportesViewModel Filtro { get; set; } = new();
        public double IndiceRotacion { get; set; }
        public double TasaPrestamosActivos { get; set; }
        public double EficienciaDevoluciones { get; set; }
        public UsuarioTopViewModel? UsuarioMasMoroso { get; set; }
        public LibroTopViewModel? LibroMasSolicitado { get; set; }
        public CategoriaPopularViewModel? CategoriaMasPopular { get; set; }
        public ReportesExportOptions ExportOptions { get; set; } = new();
        public int TotalCursosConPrestamos { get; set; }
        public CursoActividadViewModel? CursoMasActivo { get; set; }
        public CursoActividadViewModel? CursoMayorMora { get; set; }
        public List<CursoActividadViewModel> CursosTop { get; set; } = new();
        public List<CursoActividadViewModel> CursosConRiesgo { get; set; } = new();
        public string? GeneradoPor { get; set; }
        public ReportesChartViewModel ChartData { get; set; } = new();
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
        public string? Segmento { get; set; }
        public int MesNumero { get; set; }
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

    public class ReportesExportOptions
    {
        public bool IncluirResumen { get; set; } = true;
        public bool IncluirPrestamos { get; set; } = true;
        public bool IncluirRankings { get; set; } = true;
        public bool IncluirEstadisticas { get; set; } = true;
        public bool IncluirAlertas { get; set; } = true;
    }

    public class CursoActividadViewModel
    {
        public string Curso { get; set; } = "Sin curso";
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        public int PrestamosVencidos { get; set; }
        public double PorcentajeDelTotal { get; set; }
        public double PromedioDiasPrestamo { get; set; }
        public List<CursoLibroResumenViewModel> LibrosFavoritos { get; set; } = new();
    }

    public class CursoLibroResumenViewModel
    {
        public string LibroTitulo { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
    }

    public class ReportesChartViewModel
    {
        public List<string> TendenciaLabels { get; set; } = new();
        public List<ChartSerieViewModel> TendenciaSeries { get; set; } = new();
        public List<string> CategoriasLabels { get; set; } = new();
        public List<int> CategoriasValores { get; set; } = new();
        public List<string> EstadoLabels { get; set; } = new();
        public List<int> EstadoValores { get; set; } = new();
    }

    public class ChartSerieViewModel
    {
        public string Label { get; set; } = string.Empty;
        public List<int> Valores { get; set; } = new();
    }
}

