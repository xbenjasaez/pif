using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class EjemplaresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EjemplaresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ejemplares/Index?libroId=5
        public async Task<IActionResult> Index(int? libroId, string? busqueda, string? estado, string? orden = "recientes")
        {
            var query = _context.Ejemplares
                .Include(e => e.Libro)
                .AsQueryable();

            string libroTitulo = "Todos los libros";

            if (libroId.HasValue)
            {
                query = query.Where(e => e.LibroId == libroId.Value);
                
                var libro = await _context.Libros.FindAsync(libroId.Value);
                libroTitulo = libro?.Titulo ?? "Libro";
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = $"%{busqueda.Trim()}%";
                query = query.Where(e =>
                    EF.Functions.Like(e.CodigoBarras, term) ||
                    (e.Libro != null && (EF.Functions.Like(e.Libro.Titulo, term) || EF.Functions.Like(e.Libro.Autor ?? string.Empty, term))) ||
                    (e.PrestadoA != null && EF.Functions.Like(e.PrestadoA, term)));
            }

            var estadosDisponibles = new[] { "Todos", "Disponible", "Prestado", "Reservado", "En Reparacion", "Extraviado", "Dado de baja" };
            var estadoFiltrado = estado;
            if (!string.IsNullOrWhiteSpace(estadoFiltrado) && estadoFiltrado != "Todos")
            {
                query = query.Where(e => e.Estado == estadoFiltrado);
            }

            var ordenNormalizado = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            query = ordenNormalizado switch
            {
                "codigo" => query.OrderBy(e => e.CodigoBarras),
                "codigo_desc" => query.OrderByDescending(e => e.CodigoBarras),
                "estado" => query.OrderBy(e => e.Estado).ThenBy(e => e.CodigoBarras),
                "libro" => query.OrderBy(e => e.Libro != null ? e.Libro.Titulo : string.Empty).ThenBy(e => e.CodigoBarras),
                "prestamo" => query.OrderByDescending(e => e.FechaPrestamo ?? DateTime.MinValue),
                _ => query.OrderByDescending(e => e.FechaAgregado)
            };

            var lista = await query.AsNoTracking().ToListAsync();

            var resumen = new EjemplaresResumenViewModel
            {
                Total = lista.Count,
                Disponibles = lista.Count(e => e.Estado == "Disponible"),
                Prestados = lista.Count(e => e.Estado == "Prestado"),
                Otros = lista.Count(e => e.Estado != "Disponible" && e.Estado != "Prestado")
            };

            var viewModel = new EjemplaresIndexViewModel
            {
                Ejemplares = lista,
                LibroId = libroId,
                LibroTitulo = libroTitulo,
                Busqueda = busqueda,
                EstadoSeleccionado = estadoFiltrado,
                OrdenSeleccionado = ordenNormalizado,
                EstadosDisponibles = estadosDisponibles,
                Resumen = resumen
            };

            return View(viewModel);
        }

        // GET: Ejemplares/Create?libroId=5
        public async Task<IActionResult> Create(int? libroId)
        {
            if (libroId.HasValue)
            {
                var libro = await _context.Libros.FindAsync(libroId.Value);
                if (libro == null)
                {
                    return NotFound();
                }
                ViewBag.LibroId = libroId.Value;
                ViewBag.LibroTitulo = libro.Titulo;
            }
            else
            {
                ViewBag.Libros = await _context.Libros
                    .OrderBy(l => l.Titulo)
                    .Select(l => new { l.Id, l.Titulo, l.Autor })
                    .ToListAsync();
            }

            return View();
        }

        // POST: Ejemplares/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LibroId,CodigoBarras,Estado,Notas")] Ejemplar ejemplar)
        {
            if (ejemplar.LibroId <= 0)
            {
                ModelState.AddModelError("LibroId", "Debe seleccionar un libro válido.");
            }
            
            ejemplar.CodigoBarras = ejemplar.CodigoBarras?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(ejemplar.CodigoBarras))
            {
                ejemplar.CodigoBarras = await GenerarCodigoBarrasUnico();
            }
            else
            {
                var codigoExistente = await _context.Ejemplares
                    .AnyAsync(e => e.CodigoBarras == ejemplar.CodigoBarras);
                
                if (codigoExistente)
                {
                    ModelState.AddModelError("CodigoBarras", "Ya existe un ejemplar con este código de barras.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ejemplar.FechaAgregado = DateTime.Now;
                    if (string.IsNullOrEmpty(ejemplar.Estado))
                    {
                        ejemplar.Estado = "Disponible";
                    }
                    
                    _context.Add(ejemplar);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Ejemplar creado correctamente. Código de barras: {ejemplar.CodigoBarras}";
                    
                    if (ejemplar.LibroId > 0)
                    {
                        return RedirectToAction(nameof(Index), new { libroId = ejemplar.LibroId });
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al guardar el ejemplar: {ex.InnerException?.Message ?? ex.Message}";
                    if (ex.InnerException?.Message?.Contains("UNIQUE") == true || ex.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("CodigoBarras", "Ya existe un ejemplar con este código de barras.");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el ejemplar: {ex.Message}";
                }
            }

            // Recargar datos necesarios para la vista
            if (ejemplar.LibroId > 0)
            {
                var libro = await _context.Libros.FindAsync(ejemplar.LibroId);
                if (libro != null)
                {
                    ejemplar.Libro = libro; // Asignar el libro para evitar null reference
                }
                ViewBag.LibroId = ejemplar.LibroId;
                ViewBag.LibroTitulo = libro?.Titulo ?? "Libro";
            }
            else
            {
                ViewBag.Libros = await _context.Libros
                    .OrderBy(l => l.Titulo)
                    .Select(l => new { l.Id, l.Titulo, l.Autor })
                    .ToListAsync();
            }

            return View(ejemplar);
        }

        // GET: Ejemplares/CrearMultiple?libroId=5
        public async Task<IActionResult> CrearMultiple(int? libroId)
        {
            if (!libroId.HasValue)
            {
                return NotFound();
            }

            var libro = await _context.Libros.FindAsync(libroId.Value);
            if (libro == null)
            {
                return NotFound();
            }

            ViewBag.LibroId = libroId.Value;
            ViewBag.LibroTitulo = libro.Titulo;
            return View();
        }

        // POST: Ejemplares/CrearMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearMultiple(int libroId, int cantidad, string? prefijoCodigo)
        {
            if (cantidad <= 0 || cantidad > 100)
            {
                TempData["ErrorMessage"] = "La cantidad debe estar entre 1 y 100.";
                return RedirectToAction(nameof(Create), new { libroId });
            }

            var libro = await _context.Libros.FindAsync(libroId);
            if (libro == null)
            {
                return NotFound();
            }

            var ejemplaresCreados = 0;
            var errores = new List<string>();

            for (int i = 0; i < cantidad; i++)
            {
                try
                {
                    var codigoBarras = string.IsNullOrEmpty(prefijoCodigo) 
                        ? await GenerarCodigoBarrasUnico()
                        : $"{prefijoCodigo}{i + 1:D6}";

                    // Verificar que el código no exista
                    var existe = await _context.Ejemplares
                        .AnyAsync(e => e.CodigoBarras == codigoBarras);
                    
                    if (existe)
                    {
                        codigoBarras = await GenerarCodigoBarrasUnico();
                    }

                    var ejemplar = new Ejemplar
                    {
                        LibroId = libroId,
                        CodigoBarras = codigoBarras,
                        Estado = "Disponible",
                        FechaAgregado = DateTime.Now
                    };

                    _context.Add(ejemplar);
                    ejemplaresCreados++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Error al crear ejemplar {i + 1}: {ex.Message}");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Se crearon {ejemplaresCreados} ejemplar(es) correctamente para el libro '{libro.Titulo}'.";
                
                if (errores.Any())
                {
                    TempData["WarningMessage"] = $"Algunos ejemplares no se pudieron crear: {string.Join(", ", errores)}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al guardar los ejemplares: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { libroId });
        }

        // GET: Ejemplares/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ejemplar == null)
            {
                return NotFound();
            }

            return View(ejemplar);
        }

        // POST: Ejemplares/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LibroId,CodigoBarras,Estado,Notas")] Ejemplar ejemplar)
        {
            if (id != ejemplar.Id)
            {
                return NotFound();
            }

            // Limpiar espacios en blanco del código de barras
            ejemplar.CodigoBarras = ejemplar.CodigoBarras?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(ejemplar.CodigoBarras))
            {
                ModelState.AddModelError("CodigoBarras", "El código de barras es obligatorio.");
            }
            else
            {
                // Validar que sea único (excepto el actual)
                var codigoExistente = await _context.Ejemplares
                    .AnyAsync(e => e.CodigoBarras == ejemplar.CodigoBarras && e.Id != ejemplar.Id);
                
                if (codigoExistente)
                {
                    ModelState.AddModelError("CodigoBarras", "Ya existe otro ejemplar con este código de barras.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ejemplarOriginal = await _context.Ejemplares.FindAsync(id);
                    if (ejemplarOriginal == null)
                    {
                        return NotFound();
                    }

                    ejemplarOriginal.CodigoBarras = ejemplar.CodigoBarras;
                    ejemplarOriginal.Estado = ejemplar.Estado;
                    ejemplarOriginal.Notas = ejemplar.Notas;

                    _context.Entry(ejemplarOriginal).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Ejemplar actualizado correctamente.";
                    return RedirectToAction(nameof(Index), new { libroId = ejemplar.LibroId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EjemplarExists(ejemplar.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al actualizar el ejemplar: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado: {ex.Message}";
                }
            }

            // Recargar el ejemplar con el Libro incluido para mostrar en la vista
            var ejemplarConLibro = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == ejemplar.Id);
            
            if (ejemplarConLibro != null)
            {
                // Copiar los valores del formulario al ejemplar cargado
                ejemplarConLibro.CodigoBarras = ejemplar.CodigoBarras;
                ejemplarConLibro.Estado = ejemplar.Estado;
                ejemplarConLibro.Notas = ejemplar.Notas;
                return View(ejemplarConLibro);
            }
            
            return View(ejemplar);
        }

        // GET: Ejemplares/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ejemplar == null)
            {
                return NotFound();
            }

            // Verificar préstamos activos
            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Usuario)
                .Where(p => p.EjemplarId == id && p.Estado == "Activo")
                .ToListAsync();

            if (prestamosActivos.Any())
            {
                ViewBag.PrestamosActivos = prestamosActivos;
                TempData["WarningMessage"] = $"⚠️ Este ejemplar tiene {prestamosActivos.Count} préstamo(s) activo(s). Debe devolverlos primero.";
            }

            return View(ejemplar);
        }

        // POST: Ejemplares/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ejemplar == null)
            {
                return NotFound();
            }

            // Verificar préstamos activos
            var prestamosActivos = await _context.Prestamos
                .AnyAsync(p => p.EjemplarId == id && p.Estado == "Activo");

            if (prestamosActivos)
            {
                TempData["ErrorMessage"] = "No se puede eliminar el ejemplar porque tiene préstamos activos. Debe devolverlos primero.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var libroId = ejemplar.LibroId;
            
            try
            {
                _context.Ejemplares.Remove(ejemplar);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ejemplar eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el ejemplar: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index), new { libroId });
        }

        // GET: Ejemplares/GenerarDesdeLibro/5
        public async Task<IActionResult> GenerarDesdeLibro(int libroId)
        {
            var libro = await _context.Libros.FindAsync(libroId);
            if (libro == null)
            {
                return NotFound();
            }

            ViewBag.Libro = libro;
            return View();
        }

        // POST: Ejemplares/GenerarDesdeLibro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarDesdeLibro(int libroId, int cantidad)
        {
            return await CrearMultiple(libroId, cantidad, null);
        }

        private bool EjemplarExists(int id)
        {
            return _context.Ejemplares.Any(e => e.Id == id);
        }

        private async Task<string> GenerarCodigoBarrasUnico()
        {
            var random = new Random();
            string codigo;
            bool existe;
            
            do
            {
                codigo = "EJ" + DateTime.Now.ToString("yyyyMMdd") + random.Next(1000, 9999).ToString();
                existe = await _context.Ejemplares.AnyAsync(e => e.CodigoBarras == codigo);
            } while (existe);

            return codigo;
        }
    }
}

