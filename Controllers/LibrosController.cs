using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Helpers;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class LibrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LibrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Libros
        public async Task<IActionResult> Index(string? searchString, string? categoria, string? estado, int page = 1, int pageSize = 20)
        {
            var libros = _context.Libros.Include(l => l.Proveedor).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                libros = libros.Where(l => l.Titulo.Contains(searchString) || 
                                         l.Autor.Contains(searchString) || 
                                         l.ISBN!.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(categoria))
            {
                libros = libros.Where(l => l.Categoria == categoria);
            }

            if (!string.IsNullOrEmpty(estado))
            {
                libros = libros.Where(l => l.Estado == estado);
            }

            var categorias = await _context.Libros
                .Where(l => !string.IsNullOrEmpty(l.Categoria))
                .Select(l => l.Categoria!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.SearchString = searchString;
            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.EstadoSeleccionado = estado;

            // Ordenar por ubicación (A-Z, luego por número de repisa) antes de paginar
            var librosOrdenados = libros.AsEnumerable().OrderBy(l => {
                if (string.IsNullOrEmpty(l.Ubicacion))
                    return "ZZ-999"; // Los sin ubicación van al final
                
                var match = System.Text.RegularExpressions.Regex.Match(l.Ubicacion, @"^([A-Z])-?(\d+)$");
                if (match.Success)
                {
                    var letra = match.Groups[1].Value;
                    var numero = int.Parse(match.Groups[2].Value);
                    return $"{letra}-{numero:D3}"; // Formato para ordenar correctamente
                }
                return l.Ubicacion; // Si no coincide el formato, ordenar alfabéticamente
            }).ToList();

            // Contar total para paginación
            var totalItems = librosOrdenados.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Asegurar que la página sea válida
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Obtener solo los elementos de la página actual
            var librosList = librosOrdenados
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Obtener conteo de ejemplares disponibles por libro
            var librosIds = librosList.Select(l => l.Id).ToList();
            var ejemplaresDisponibles = await _context.Ejemplares
                .Where(e => librosIds.Contains(e.LibroId) && e.Estado == "Disponible")
                .GroupBy(e => e.LibroId)
                .Select(g => new { LibroId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.LibroId, x => x.Cantidad);

            ViewBag.EjemplaresDisponibles = ejemplaresDisponibles;
            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(librosList);
        }

        // GET: Libros/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var libro = await _context.Libros
                .Include(l => l.Proveedor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (libro == null)
            {
                return NotFound();
            }

            return View(libro);
        }

        // GET: Libros/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Proveedores = await _context.Proveedores
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            return View();
        }

        // POST: Libros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,Autor,ISBN,Categoria,Año,Editorial,Descripcion,Ubicacion,Estado,Notas,ProveedorId")] Libro libro)
        {
            // Validar ISBN duplicado si se proporciona
            if (!string.IsNullOrEmpty(libro.ISBN))
            {
                var isbnExistente = await _context.Libros
                    .AnyAsync(l => l.ISBN == libro.ISBN && l.Id != libro.Id);
                
                if (isbnExistente)
                {
                    ModelState.AddModelError("ISBN", "Ya existe un libro con este ISBN.");
                }
            }

            if (!string.IsNullOrWhiteSpace(libro.CodigoBarras))
            {
                if (Ean13Helper.TryNormalize(libro.CodigoBarras, out var codigoNormalizado, out var errorCodigo))
                {
                    var codigoDuplicado = await _context.Libros
                        .AnyAsync(l => l.CodigoBarras == codigoNormalizado && l.Id != libro.Id);

                    if (codigoDuplicado)
                    {
                        ModelState.AddModelError("CodigoBarras", "Ya existe otro libro con este código de barras.");
                    }
                    else
                    {
                        libro.CodigoBarras = codigoNormalizado;
                    }
                }
                else
                {
                    ModelState.AddModelError("CodigoBarras", errorCodigo);
                }
            }
            else
            {
                ModelState.AddModelError("CodigoBarras", "El código de barras es obligatorio.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    libro.FechaAgregado = DateTime.Now;
                    libro.CodigoBarras = await GenerarCodigoBarras();
                    _context.Add(libro);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Libro agregado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al guardar el libro: {ex.InnerException?.Message ?? ex.Message}";
                    if (ex.InnerException?.Message?.Contains("UNIQUE") == true || ex.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("", "Ya existe un libro con los mismos datos. Verifica el ISBN o código de barras.");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el libro: {ex.Message}";
                }
            }

            ViewBag.Proveedores = await _context.Proveedores
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            return View(libro);
        }

        // GET: Libros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
            {
                return NotFound();
            }

            // Verificar si el libro está prestado para mostrar advertencia
            if (libro.Estado == "Prestado")
            {
                var prestamoActivo = await _context.Prestamos
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.LibroId == id && p.Estado == "Activo");

                if (prestamoActivo != null)
                {
                    ViewBag.PrestamoActivo = prestamoActivo;
                    ViewBag.MostrarAdvertencia = true;
                }
            }

            ViewBag.Proveedores = await _context.Proveedores
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            return View(libro);
        }

        // POST: Libros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Autor,ISBN,Categoria,Año,Editorial,Descripcion,Ubicacion,Estado,Notas,ProveedorId,CodigoBarras,FechaAgregado")] Libro libro, string? confirmarDevolucion)
        {
            if (id != libro.Id)
            {
                return NotFound();
            }

            // Obtener el estado original del libro antes de los cambios
            var libroOriginal = await _context.Libros.FindAsync(id);
            if (libroOriginal == null)
            {
                return NotFound();
            }

            // Guardar el estado original para comparación posterior
            var estadoOriginal = libroOriginal.Estado;

            // Verificar si se está cambiando de "Prestado" a "Disponible"
            if (estadoOriginal == "Prestado" && libro.Estado == "Disponible")
            {
                // Buscar préstamo activo asociado a este libro
                var prestamoActivo = await _context.Prestamos
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.LibroId == id && p.Estado == "Activo");

                if (prestamoActivo != null)
                {
                    // Si no se ha confirmado la devolución, mostrar advertencia
                    if (string.IsNullOrEmpty(confirmarDevolucion) || confirmarDevolucion != "true")
                    {
                        ViewBag.Proveedores = await _context.Proveedores
                            .OrderBy(p => p.Nombre)
                            .Select(p => new { p.Id, p.Nombre })
                            .ToListAsync();
                        
                        ViewBag.PrestamoActivo = prestamoActivo;
                        ViewBag.MostrarAdvertencia = true;
                        
                        // Usar ViewData en lugar de TempData para que persista
                        ViewData["WarningMessage"] = $"⚠️ Este libro está actualmente prestado a {prestamoActivo.Usuario.NombreCompleto} (RUT: {prestamoActivo.Usuario.RUT}). Si cambias el estado a 'Disponible', el préstamo se marcará como devuelto automáticamente. Asegúrate de que el usuario haya devuelto físicamente el libro antes de continuar.";
                        
                        return View(libro);
                    }
                    else
                    {
                        // Confirmado: devolver el préstamo automáticamente
                        prestamoActivo.Estado = "Devuelto";
                        prestamoActivo.FechaDevolucion = DateTime.Now;
                        _context.Entry(prestamoActivo).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                        // Actualizar contador del usuario
                        var usuario = prestamoActivo.Usuario;
                        if (usuario.PrestamosActivos > 0)
                        {
                            usuario.PrestamosActivos--;
                        }
                        _context.Entry(usuario).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                        // Limpiar campos de préstamo del libro
                        libro.PrestadoA = null;
                        libro.FechaPrestamo = null;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualizar el libro original con los cambios del modelo
                    libroOriginal.Titulo = libro.Titulo;
                    libroOriginal.Autor = libro.Autor;
                    libroOriginal.ISBN = libro.ISBN;
                    libroOriginal.Categoria = libro.Categoria;
                    libroOriginal.Año = libro.Año;
                    libroOriginal.Editorial = libro.Editorial;
                    libroOriginal.Descripcion = libro.Descripcion;
                    libroOriginal.Ubicacion = libro.Ubicacion;
                    libroOriginal.Estado = libro.Estado; // Asegurar que el estado se actualice
                    libroOriginal.Notas = libro.Notas;
                    libroOriginal.ProveedorId = libro.ProveedorId;
                    libroOriginal.CodigoBarras = libro.CodigoBarras;
                    libroOriginal.FechaAgregado = libro.FechaAgregado;
                    
                    // Si ya se limpiaron PrestadoA y FechaPrestamo (cuando se confirmó la devolución)
                    if (libro.PrestadoA == null && libro.FechaPrestamo == null)
                    {
                        libroOriginal.PrestadoA = null;
                        libroOriginal.FechaPrestamo = null;
                    }
                    
                    _context.Entry(libroOriginal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    await _context.SaveChangesAsync();
                    
                    // Verificar si cambió de Prestado a Disponible
                    if (estadoOriginal == "Prestado" && libroOriginal.Estado == "Disponible")
                    {
                        TempData["SuccessMessage"] = $"Libro actualizado correctamente. El préstamo asociado ha sido marcado como devuelto automáticamente.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Libro actualizado correctamente.";
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LibroExists(libro.Id))
                    {
                        TempData["ErrorMessage"] = "El libro fue eliminado por otro usuario.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "El libro fue modificado por otro usuario. Por favor, recarga la página e intenta de nuevo.";
                        ViewBag.Proveedores = await _context.Proveedores
                            .OrderBy(p => p.Nombre)
                            .Select(p => new { p.Id, p.Nombre })
                            .ToListAsync();
                        return View(libro);
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al guardar el libro: {ex.InnerException?.Message ?? ex.Message}";
                    ViewBag.Proveedores = await _context.Proveedores
                        .OrderBy(p => p.Nombre)
                        .Select(p => new { p.Id, p.Nombre })
                        .ToListAsync();
                    return View(libro);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el libro: {ex.Message}";
                    ViewBag.Proveedores = await _context.Proveedores
                        .OrderBy(p => p.Nombre)
                        .Select(p => new { p.Id, p.Nombre })
                        .ToListAsync();
                    return View(libro);
                }
            }

            ViewBag.Proveedores = await _context.Proveedores
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            return View(libro);
        }

        // GET: Libros/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var libro = await _context.Libros
                .Include(l => l.Proveedor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (libro == null)
            {
                return NotFound();
            }

            // Verificar préstamos activos para mostrar advertencia
            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Usuario)
                .Where(p => p.LibroId == id && p.Estado == "Activo")
                .ToListAsync();

            if (prestamosActivos.Any())
            {
                ViewBag.PrestamosActivos = prestamosActivos;
                TempData["WarningMessage"] = $"⚠️ Este libro tiene {prestamosActivos.Count} préstamo(s) activo(s). Debe devolverlos primero antes de eliminar el libro.";
            }

            return View(libro);
        }

        // POST: Libros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro != null)
            {
                try
                {
                    // Verificar si tiene préstamos activos
                    var prestamosActivos = await _context.Prestamos
                        .Include(p => p.Usuario)
                        .Where(p => p.LibroId == id && p.Estado == "Activo")
                        .ToListAsync();

                    if (prestamosActivos.Any())
                    {
                        var usuarios = string.Join(", ", prestamosActivos.Select(p => p.Usuario.NombreCompleto));
                        TempData["ErrorMessage"] = $"No se puede eliminar el libro porque tiene préstamos activos a los siguientes usuarios: {usuarios}. Debe devolver los préstamos primero desde el apartado 'Préstamos' o 'Registro de Circulación'.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Verificar si tiene préstamos históricos (aunque estén devueltos)
                    var prestamosHistoricos = await _context.Prestamos
                        .AnyAsync(p => p.LibroId == id);

                    if (prestamosHistoricos)
                    {
                        TempData["WarningMessage"] = "Este libro tiene un historial de préstamos. Se eliminará el libro pero se mantendrá el registro de préstamos para auditoría.";
                    }

                    _context.Libros.Remove(libro);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Libro eliminado correctamente.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al eliminar el libro: {ex.InnerException?.Message ?? ex.Message}. Puede que tenga relaciones con otros datos.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al eliminar el libro: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LibroExists(int id)
        {
            return _context.Libros.Any(e => e.Id == id);
        }

        // GET: Libros/SincronizarEstados
        [HttpGet]
        public async Task<IActionResult> SincronizarEstados()
        {
            try
            {
                var ejemplaresCorregidos = 0;
                var prestamosActivos = await _context.Prestamos
                    .Where(p => p.Estado == "Activo")
                    .Include(p => p.Ejemplar)
                        .ThenInclude(e => e.Libro)
                    .Include(p => p.Usuario)
                    .ToListAsync();

                var ejemplaresIdsPrestados = prestamosActivos
                    .Where(p => p.EjemplarId > 0)
                    .Select(p => p.EjemplarId)
                    .Distinct()
                    .ToList();

                // Marcar como Disponible los ejemplares que no tienen préstamos activos pero están marcados como Prestado
                var ejemplaresInconsistentes = await _context.Ejemplares
                    .Where(e => e.Estado == "Prestado" && !ejemplaresIdsPrestados.Contains(e.Id))
                    .ToListAsync();

                foreach (var ejemplar in ejemplaresInconsistentes)
                {
                    ejemplar.Estado = "Disponible";
                    ejemplar.PrestadoA = null;
                    ejemplar.FechaPrestamo = null;
                    _context.Entry(ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    ejemplaresCorregidos++;
                }

                // Asegurar que los ejemplares con préstamos activos estén marcados como Prestado
                foreach (var prestamo in prestamosActivos)
                {
                    if (prestamo.EjemplarId > 0 && prestamo.Ejemplar != null)
                    {
                        if (prestamo.Ejemplar.Estado != "Prestado")
                        {
                            prestamo.Ejemplar.Estado = "Prestado";
                            prestamo.Ejemplar.PrestadoA = prestamo.Usuario?.NombreCompleto ?? "Usuario desconocido";
                            prestamo.Ejemplar.FechaPrestamo = prestamo.FechaPrestamo;
                            _context.Entry(prestamo.Ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            ejemplaresCorregidos++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sincronización completada. Se corrigieron {ejemplaresCorregidos} ejemplar(es) con estados inconsistentes.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al sincronizar estados: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error detallado en SincronizarEstados: {ex}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> GenerarCodigoBarras()
        {
            return await Ean13Helper.GenerarCodigoUnicoAsync(async codigo =>
                await _context.Libros.AnyAsync(l => l.CodigoBarras == codigo));
        }

        // GET: Libros/Catalogo
        public async Task<IActionResult> Catalogo(string? searchString, string? categoria, string? estado)
        {
            var libros = _context.Libros.Include(l => l.Proveedor).AsQueryable();

            // Solo mostrar resultados si hay algún filtro aplicado
            bool hayFiltros = !string.IsNullOrEmpty(searchString) || 
                              !string.IsNullOrEmpty(categoria) || 
                              !string.IsNullOrEmpty(estado);

            if (!hayFiltros)
            {
                // Sin filtros, no mostrar libros
                ViewBag.Categorias = await _context.Libros
                    .Where(l => !string.IsNullOrEmpty(l.Categoria))
                    .Select(l => l.Categoria!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                ViewBag.SearchString = searchString;
                ViewBag.CategoriaSeleccionada = categoria;
                ViewBag.EstadoSeleccionado = estado;
                ViewBag.EjemplaresDisponibles = new Dictionary<int, int>();
                ViewBag.TotalEjemplares = new Dictionary<int, int>();
                return View(new List<Libro>());
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                libros = libros.Where(l => l.Titulo.Contains(searchString) || 
                                         l.Autor.Contains(searchString) || 
                                         (l.ISBN != null && l.ISBN.Contains(searchString)) ||
                                         (l.Editorial != null && l.Editorial.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(categoria))
            {
                libros = libros.Where(l => l.Categoria == categoria);
            }

            if (!string.IsNullOrEmpty(estado))
            {
                // Filtrar por disponibilidad de ejemplares
                if (estado == "Disponible")
                {
                    var librosConDisponibles = await _context.Ejemplares
                        .Where(e => e.Estado == "Disponible" || e.Estado == "Deteriorado")
                        .Select(e => e.LibroId)
                        .Distinct()
                        .ToListAsync();
                    libros = libros.Where(l => librosConDisponibles.Contains(l.Id));
                }
                else if (estado == "Prestado")
                {
                    var librosSinDisponibles = await _context.Ejemplares
                        .GroupBy(e => e.LibroId)
                        .Where(g => !g.Any(e => e.Estado == "Disponible" || e.Estado == "Deteriorado"))
                        .Select(g => g.Key)
                        .ToListAsync();
                    libros = libros.Where(l => librosSinDisponibles.Contains(l.Id));
                }
            }

            var categorias = await _context.Libros
                .Where(l => !string.IsNullOrEmpty(l.Categoria))
                .Select(l => l.Categoria!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.SearchString = searchString;
            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.EstadoSeleccionado = estado;

            var librosList = await libros.OrderBy(l => l.Titulo).Take(50).ToListAsync();
            
            // Obtener conteo de ejemplares disponibles y totales por libro
            var librosIds = librosList.Select(l => l.Id).ToList();
            
            var ejemplaresDisponibles = await _context.Ejemplares
                .Where(e => librosIds.Contains(e.LibroId) && (e.Estado == "Disponible" || e.Estado == "Deteriorado"))
                .GroupBy(e => e.LibroId)
                .Select(g => new { LibroId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.LibroId, x => x.Cantidad);

            var totalEjemplares = await _context.Ejemplares
                .Where(e => librosIds.Contains(e.LibroId))
                .GroupBy(e => e.LibroId)
                .Select(g => new { LibroId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.LibroId, x => x.Cantidad);

            ViewBag.EjemplaresDisponibles = ejemplaresDisponibles;
            ViewBag.TotalEjemplares = totalEjemplares;

            return View(librosList);
        }

        // GET: Libros/ObtenerEjemplares/5
        [HttpGet]
        public async Task<IActionResult> ObtenerEjemplares(int id)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
            {
                return Json(new { success = false, message = "Libro no encontrado" });
            }

            var ejemplares = await _context.Ejemplares
                .Where(e => e.LibroId == id)
                .OrderBy(e => e.Estado == "Disponible" ? 0 : e.Estado == "Deteriorado" ? 1 : 2)
                .ThenBy(e => e.CodigoBarras)
                .Select(e => new {
                    id = e.Id,
                    codigoBarras = e.CodigoBarras,
                    estado = e.Estado,
                    ubicacion = e.Ubicacion,
                    prestadoA = e.PrestadoA,
                    notas = e.Notas,
                    fechaPrestamo = e.FechaPrestamo
                })
                .ToListAsync();

            return Json(new { 
                success = true, 
                ejemplares = ejemplares,
                libro = new {
                    id = libro.Id,
                    titulo = libro.Titulo,
                    autor = libro.Autor
                }
            });
        }
    }
}
