using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Services;

namespace BibliotecaVirtualWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlertaSistemaService _alertas;

        public HomeController(ApplicationDbContext context, IAlertaSistemaService alertas)
        {
            _context = context;
            _alertas = alertas;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var totalEjemplares = await _context.Ejemplares.CountAsync();
            var ejemplaresDisponibles = await _context.Ejemplares.CountAsync(e => e.Estado == "Disponible");
            
                var rangoMeses = Enumerable.Range(0, 6)
                    .Select(offset => DateTime.Today.AddMonths(-5 + offset))
                    .Select(fecha => new DateTime(fecha.Year, fecha.Month, 1))
                    .ToList();

                var inicioSerie = rangoMeses.First();

                var prestamosPorMes = await _context.Prestamos
                    .Where(p => p.FechaPrestamo >= inicioSerie)
                    .GroupBy(p => new { p.FechaPrestamo.Year, p.FechaPrestamo.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() })
                    .ToListAsync();

                var devolucionesPorMes = await _context.Prestamos
                    .Where(p => p.FechaDevolucion != null && p.FechaDevolucion >= inicioSerie)
                    .GroupBy(p => new { p.FechaDevolucion!.Value.Year, p.FechaDevolucion!.Value.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() })
                    .ToListAsync();

                var etiquetas = new List<string>();
                var seriePrestamos = new List<int>();
                var serieDevoluciones = new List<int>();

                foreach (var mes in rangoMeses)
                {
                    etiquetas.Add(mes.ToString("MMM"));
                    var prestamoMes = prestamosPorMes.FirstOrDefault(p => p.Year == mes.Year && p.Month == mes.Month);
                    var devolucionMes = devolucionesPorMes.FirstOrDefault(p => p.Year == mes.Year && p.Month == mes.Month);
                    seriePrestamos.Add(prestamoMes?.Total ?? 0);
                    serieDevoluciones.Add(devolucionMes?.Total ?? 0);
                }

                var hoy = DateTime.Today;
                var manana = hoy.AddDays(1);

                var dashboardData = new DashboardViewModel
                {
                    TotalLibros = await _context.Libros.CountAsync(),
                    TotalEjemplares = totalEjemplares,
                    LibrosDisponibles = ejemplaresDisponibles,
                    LibrosPrestados = await _context.Ejemplares.CountAsync(e => e.Estado == "Prestado"),
                    TotalUsuarios = await _context.Usuarios.CountAsync(),
                    PrestamosActivos = await _context.Prestamos.CountAsync(p => p.Estado == "Activo"),
                    PrestamosVencidos = await _context.Prestamos.CountAsync(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now),
                    EjemplaresDeteriorados = await _context.Ejemplares.CountAsync(e => e.Estado == "Deteriorado"),
                    PrestamosHoy = await _context.Prestamos.CountAsync(p => p.FechaPrestamo >= hoy && p.FechaPrestamo < manana),
                    DevolucionesHoy = await _context.Prestamos.CountAsync(p => p.FechaDevolucion != null && p.FechaDevolucion >= hoy && p.FechaDevolucion < manana),
                    LibrosRecientes = await _context.Libros
                        .OrderByDescending(l => l.FechaAgregado)
                        .Take(5)
                        .Select(l => new LibroResumenViewModel
                        {
                            Id = l.Id,
                            Titulo = l.Titulo,
                            Autor = l.Autor,
                            Estado = l.Estado,
                            EstadoBadgeClass = l.EstadoBadgeClass
                        })
                        .ToListAsync(),
                    PrestamosPorVencer = (await _context.Prestamos
                        .Where(p => p.Estado == "Activo" && p.FechaVencimiento <= DateTime.Now.AddDays(3) && p.FechaVencimiento >= DateTime.Now)
                        .Include(p => p.Ejemplar)
                            .ThenInclude(e => e.Libro)
                        .Include(p => p.Usuario)
                        .OrderBy(p => p.FechaVencimiento)
                        .Take(10)
                        .ToListAsync())
                        .Select(p => new PrestamoResumenViewModel
                        {
                            Id = p.Id,
                            TituloLibro = p.Ejemplar?.Libro?.Titulo ?? "Sin título",
                            NombreUsuario = p.Usuario?.NombreCompleto ?? "Usuario desconocido",
                            Curso = p.Usuario?.Curso != null ? p.Usuario.Curso + (p.Usuario.LetraCurso ?? "") : null,
                            CodigoBarras = p.Ejemplar?.CodigoBarras,
                            FechaVencimiento = p.FechaVencimiento,
                            DiasRestantes = (int)(p.FechaVencimiento - DateTime.Now).TotalDays,
                            EstadoBadgeClass = p.EstadoBadgeClass
                        })
                        .ToList(),
                    PrestamosVencidosHoy = (await _context.Prestamos
                        .Where(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now)
                        .Include(p => p.Ejemplar)
                            .ThenInclude(e => e.Libro)
                        .Include(p => p.Usuario)
                        .OrderBy(p => p.FechaVencimiento)
                        .Take(10)
                        .ToListAsync())
                        .Select(p => new PrestamoResumenViewModel
                        {
                            Id = p.Id,
                            TituloLibro = p.Ejemplar?.Libro?.Titulo ?? "Sin título",
                            NombreUsuario = p.Usuario?.NombreCompleto ?? "Usuario desconocido",
                            Curso = p.Usuario?.Curso != null ? p.Usuario.Curso + (p.Usuario.LetraCurso ?? "") : null,
                            CodigoBarras = p.Ejemplar?.CodigoBarras,
                            FechaVencimiento = p.FechaVencimiento,
                            DiasVencido = (int)(DateTime.Now - p.FechaVencimiento).TotalDays,
                            EstadoBadgeClass = "badge bg-danger"
                        })
                        .ToList(),
                    EjemplaresDeterioradosLista = await _context.Ejemplares
                        .Where(e => e.Estado == "Deteriorado")
                        .Include(e => e.Libro)
                        .OrderByDescending(e => e.FechaAgregado)
                        .Take(10)
                        .Select(e => new EjemplarDeterioradoViewModel
                        {
                            Id = e.Id,
                            CodigoBarras = e.CodigoBarras,
                            TituloLibro = e.Libro.Titulo,
                            Ubicacion = e.Ubicacion,
                            Notas = e.Notas,
                            PrestadoA = e.PrestadoA
                        })
                        .ToListAsync()
                };

                dashboardData.AlertasActivas = await _alertas.ObtenerAlertasActivasAsync(5);
                dashboardData.KpiLabels = etiquetas;
                dashboardData.KpiPrestamos = seriePrestamos;
                dashboardData.KpiDevoluciones = serieDevoluciones;
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                await _alertas.RegistrarErrorAsync(
                    "Error en dashboard",
                    ex.Message,
                    ex.StackTrace);
                
                // Si es un error de base de datos, mostrar mensaje específico
                if (ex.Message.Contains("Table") || ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    TempData["ErrorMessage"] = "Las tablas no existen en la base de datos. Por favor, crea las tablas usando el script crear_tablas_mysql.sql en phpMyAdmin.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error al cargar los datos: {ex.Message}";
                }
                
                // Retornar vista de error o redirigir
                throw; // Re-lanzar para que el ExceptionHandler lo capture
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }

    public class DashboardViewModel
    {
        public int TotalLibros { get; set; }
        public int TotalEjemplares { get; set; }
        public int LibrosDisponibles { get; set; }
        public int LibrosPrestados { get; set; }
        public int TotalUsuarios { get; set; }
        public int PrestamosActivos { get; set; }
        public int PrestamosVencidos { get; set; }
        public int EjemplaresDeteriorados { get; set; }
        public int PrestamosHoy { get; set; }
        public int DevolucionesHoy { get; set; }
        public List<LibroResumenViewModel> LibrosRecientes { get; set; } = new();
        public List<PrestamoResumenViewModel> PrestamosPorVencer { get; set; } = new();
        public List<PrestamoResumenViewModel> PrestamosVencidosHoy { get; set; } = new();
        public List<EjemplarDeterioradoViewModel> EjemplaresDeterioradosLista { get; set; } = new();
        public IEnumerable<SistemaAlerta> AlertasActivas { get; set; } = Enumerable.Empty<SistemaAlerta>();
        public IEnumerable<string> KpiLabels { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<int> KpiPrestamos { get; set; } = Enumerable.Empty<int>();
        public IEnumerable<int> KpiDevoluciones { get; set; } = Enumerable.Empty<int>();
    }

    public class LibroResumenViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string EstadoBadgeClass { get; set; } = string.Empty;
    }

    public class PrestamoResumenViewModel
    {
        public int Id { get; set; }
        public string TituloLibro { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? Curso { get; set; }
        public string? CodigoBarras { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasRestantes { get; set; }
        public int DiasVencido { get; set; }
        public string EstadoBadgeClass { get; set; } = string.Empty;
    }

    public class EjemplarDeterioradoViewModel
    {
        public int Id { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string TituloLibro { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public string? Notas { get; set; }
        public string? PrestadoA { get; set; }
    }
}
