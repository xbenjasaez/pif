using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;

namespace BibliotecaVirtualWeb.Controllers
{
    /// <summary>
    /// Controlador público para que los alumnos consulten el catálogo de libros.
    /// No requiere autenticación - solo permite operaciones de lectura.
    /// </summary>
    [AllowAnonymous]
    public class CatalogoPublicoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogoPublicoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CatalogoPublico
        public async Task<IActionResult> Index(string? busqueda, string? categoria)
        {
            var viewModel = new CatalogoPublicoViewModel
            {
                Busqueda = busqueda,
                CategoriaSeleccionada = categoria
            };

            // Cargar categorías para el filtro
            viewModel.Categorias = await _context.Libros
                .Where(l => !string.IsNullOrEmpty(l.Categoria))
                .Select(l => l.Categoria!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Si no hay búsqueda, mostrar mensaje inicial
            if (string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(categoria))
            {
                viewModel.MostrarResultados = false;
                return View(viewModel);
            }

            viewModel.MostrarResultados = true;

            // Buscar libros
            var query = _context.Libros.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(l => 
                    l.Titulo.ToLower().Contains(termino) ||
                    l.Autor.ToLower().Contains(termino) ||
                    (l.ISBN != null && l.ISBN.Contains(termino)) ||
                    (l.Editorial != null && l.Editorial.ToLower().Contains(termino)));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(l => l.Categoria == categoria);
            }

            var libros = await query
                .OrderBy(l => l.Titulo)
                .Take(50) // Limitar resultados
                .ToListAsync();

            // Obtener disponibilidad de ejemplares
            var librosIds = libros.Select(l => l.Id).ToList();
            
            var disponibilidad = await _context.Ejemplares
                .Where(e => librosIds.Contains(e.LibroId))
                .GroupBy(e => e.LibroId)
                .Select(g => new {
                    LibroId = g.Key,
                    Total = g.Count(),
                    Disponibles = g.Count(e => e.Estado == "Disponible" || e.Estado == "Deteriorado")
                })
                .ToDictionaryAsync(x => x.LibroId, x => new DisponibilidadLibro 
                { 
                    Total = x.Total, 
                    Disponibles = x.Disponibles 
                });

            viewModel.Libros = libros.Select(l => new LibroPublicoViewModel
            {
                Id = l.Id,
                Titulo = l.Titulo,
                Autor = l.Autor,
                Editorial = l.Editorial,
                Categoria = l.Categoria,
                Año = l.Año,
                Descripcion = l.Descripcion,
                Disponibilidad = disponibilidad.ContainsKey(l.Id) 
                    ? disponibilidad[l.Id] 
                    : new DisponibilidadLibro()
            }).ToList();

            viewModel.TotalResultados = viewModel.Libros.Count;

            return View(viewModel);
        }

        // GET: CatalogoPublico/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var libro = await _context.Libros
                .FirstOrDefaultAsync(l => l.Id == id);

            if (libro == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Obtener ejemplares (solo información pública)
            var ejemplares = await _context.Ejemplares
                .Where(e => e.LibroId == id)
                .Select(e => new EjemplarPublicoViewModel
                {
                    Ubicacion = e.Ubicacion,
                    Estado = e.Estado,
                    EstaDisponible = e.Estado == "Disponible" || e.Estado == "Deteriorado"
                })
                .OrderByDescending(e => e.EstaDisponible)
                .ThenBy(e => e.Ubicacion)
                .ToListAsync();

            var viewModel = new DetalleLibroPublicoViewModel
            {
                Id = libro.Id,
                Titulo = libro.Titulo,
                Autor = libro.Autor,
                Editorial = libro.Editorial,
                ISBN = libro.ISBN,
                Categoria = libro.Categoria,
                Año = libro.Año,
                Descripcion = libro.Descripcion,
                Ejemplares = ejemplares,
                TotalEjemplares = ejemplares.Count,
                EjemplaresDisponibles = ejemplares.Count(e => e.EstaDisponible)
            };

            return View(viewModel);
        }
    }

    #region ViewModels para Catálogo Público

    public class CatalogoPublicoViewModel
    {
        public string? Busqueda { get; set; }
        public string? CategoriaSeleccionada { get; set; }
        public List<string> Categorias { get; set; } = new();
        public List<LibroPublicoViewModel> Libros { get; set; } = new();
        public int TotalResultados { get; set; }
        public bool MostrarResultados { get; set; }
    }

    public class LibroPublicoViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string? Editorial { get; set; }
        public string? Categoria { get; set; }
        public int? Año { get; set; }
        public string? Descripcion { get; set; }
        public DisponibilidadLibro Disponibilidad { get; set; } = new();
    }

    public class DisponibilidadLibro
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public bool HayDisponibles => Disponibles > 0;
    }

    public class DetalleLibroPublicoViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string? Editorial { get; set; }
        public string? ISBN { get; set; }
        public string? Categoria { get; set; }
        public int? Año { get; set; }
        public string? Descripcion { get; set; }
        public List<EjemplarPublicoViewModel> Ejemplares { get; set; } = new();
        public int TotalEjemplares { get; set; }
        public int EjemplaresDisponibles { get; set; }
    }

    public class EjemplarPublicoViewModel
    {
        public string? Ubicacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool EstaDisponible { get; set; }
    }

    #endregion
}

