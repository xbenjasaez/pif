using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventario
        public async Task<IActionResult> Index(string? searchString)
        {
            var resultados = new List<object>();
            
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                
                // Buscar en libros - case insensitive
                var librosEncontrados = await _context.Libros
                    .Include(l => l.Proveedor)
                    .ToListAsync();

                var librosFiltrados = librosEncontrados.Where(l => 
                    (l.Titulo != null && l.Titulo.ToLower().Contains(searchLower)) || 
                    (l.Autor != null && l.Autor.ToLower().Contains(searchLower)) ||
                    (l.ISBN != null && l.ISBN.ToLower().Contains(searchLower)) ||
                    (l.CodigoBarras != null && l.CodigoBarras.ToLower().Contains(searchLower)) ||
                    (l.Categoria != null && l.Categoria.ToLower().Contains(searchLower)) ||
                    (l.Proveedor != null && l.Proveedor.Nombre != null && l.Proveedor.Nombre.ToLower().Contains(searchLower)))
                    .ToList();

                foreach (var l in librosFiltrados)
                {
                    resultados.Add(new
                    {
                        Tipo = "Libro",
                        Id = l.Id,
                        Titulo = l.Titulo,
                        Autor = l.Autor,
                        ISBN = l.ISBN,
                        CodigoBarras = l.CodigoBarras,
                        Categoria = l.Categoria,
                        Estado = l.Estado,
                        Ubicacion = l.Ubicacion,
                        Proveedor = l.Proveedor != null ? l.Proveedor.Nombre : null,
                        TipoObjeto = "Libro"
                    });
                }

                // Buscar en ejemplares - case insensitive
                var ejemplaresEncontrados = await _context.Ejemplares
                    .Include(e => e.Libro)
                        .ThenInclude(l => l.Proveedor)
                    .ToListAsync();

                var ejemplaresFiltrados = ejemplaresEncontrados.Where(e => 
                    (e.CodigoBarras != null && e.CodigoBarras.ToLower().Contains(searchLower)) ||
                    (e.Libro != null && e.Libro.Titulo != null && e.Libro.Titulo.ToLower().Contains(searchLower)) ||
                    (e.Libro != null && e.Libro.Autor != null && e.Libro.Autor.ToLower().Contains(searchLower)) ||
                    (e.Notas != null && e.Notas.ToLower().Contains(searchLower)) ||
                    (e.PrestadoA != null && e.PrestadoA.ToLower().Contains(searchLower)))
                    .ToList();

                foreach (var e in ejemplaresFiltrados)
                {
                    resultados.Add(new
                    {
                        Tipo = "Ejemplar",
                        Id = e.Id,
                        Titulo = e.Libro?.Titulo ?? "Sin título",
                        Autor = e.Libro?.Autor ?? "Sin autor",
                        ISBN = e.Libro?.ISBN,
                        CodigoBarras = e.CodigoBarras,
                        Categoria = e.Libro?.Categoria,
                        Estado = e.Estado,
                        Ubicacion = e.Libro?.Ubicacion,
                        Proveedor = e.Libro?.Proveedor != null ? e.Libro.Proveedor.Nombre : null,
                        TipoObjeto = "Ejemplar",
                        LibroId = e.LibroId,
                        FechaAgregado = e.FechaAgregado,
                        PrestadoA = e.PrestadoA,
                        Notas = e.Notas
                    });
                }
            }

            ViewBag.SearchString = searchString;
            ViewBag.Resultados = resultados;
            ViewBag.TotalLibros = await _context.Libros.CountAsync();
            ViewBag.TotalEjemplares = await _context.Ejemplares.CountAsync();
            ViewBag.EjemplaresDisponibles = await _context.Ejemplares.CountAsync(e => e.Estado == "Disponible");
            ViewBag.EjemplaresPrestados = await _context.Ejemplares.CountAsync(e => e.Estado == "Prestado");

            return View();
        }
    }
}

