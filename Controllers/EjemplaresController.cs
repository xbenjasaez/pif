using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Helpers;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class EjemplaresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static bool _codigosMigrados;

        public EjemplaresController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_codigosMigrados)
            {
                await MigrarCodigosBarrasAsync();
            }

            await base.OnActionExecutionAsync(context, next);
        }

        // GET: Ejemplares/Index?libroId=5
        public async Task<IActionResult> Index(int? libroId, string? busqueda, string? estado, string? orden = "recientes")
        {
            var estadosDisponibles = new[] { "Todos", "Disponible", "Prestado", "Reservado", "En Reparacion", "Extraviado", "Dado de baja" };
            var ordenNormalizado = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            var estadoFiltrado = estado;

            string libroTitulo = "Todos los libros";

            // Determinar si hay filtros activos
            bool hayBusqueda = !string.IsNullOrWhiteSpace(busqueda);
            bool hayEstado = !string.IsNullOrWhiteSpace(estadoFiltrado) && estadoFiltrado != "Todos";
            bool hayFiltro = libroId.HasValue || hayBusqueda || hayEstado;

            // Si no hay filtros, mostrar solo el resumen global sin cargar ejemplares
            if (!hayFiltro)
            {
                var resumenGlobal = new EjemplaresResumenViewModel
                {
                    Total = await _context.Ejemplares.CountAsync(),
                    Disponibles = await _context.Ejemplares.CountAsync(e => e.Estado == "Disponible"),
                    Prestados = await _context.Ejemplares.CountAsync(e => e.Estado == "Prestado"),
                    Otros = await _context.Ejemplares.CountAsync(e => e.Estado != "Disponible" && e.Estado != "Prestado")
                };

                var viewModelVacio = new EjemplaresIndexViewModel
                {
                    Ejemplares = new List<Ejemplar>(),
                    LibroId = null,
                    LibroTitulo = libroTitulo,
                    Busqueda = busqueda,
                    EstadoSeleccionado = estadoFiltrado,
                    OrdenSeleccionado = ordenNormalizado,
                    EstadosDisponibles = estadosDisponibles,
                    Resumen = resumenGlobal,
                    MostrarMensajeBusqueda = true
                };

                return View(viewModelVacio);
            }

            // Si hay filtros, cargar los ejemplares correspondientes
            var query = _context.Ejemplares
                .Include(e => e.Libro)
                .AsQueryable();

            if (libroId.HasValue)
            {
                query = query.Where(e => e.LibroId == libroId.Value);
                
                var libro = await _context.Libros.FindAsync(libroId.Value);
                libroTitulo = libro?.Titulo ?? "Libro";
            }

            if (hayBusqueda)
            {
                var term = $"%{busqueda!.Trim()}%";
                query = query.Where(e =>
                    EF.Functions.Like(e.CodigoBarras, term) ||
                    (e.Libro != null && (EF.Functions.Like(e.Libro.Titulo, term) || EF.Functions.Like(e.Libro.Autor ?? string.Empty, term))) ||
                    (e.PrestadoA != null && EF.Functions.Like(e.PrestadoA, term)));
            }

            if (hayEstado)
            {
                query = query.Where(e => e.Estado == estadoFiltrado);
            }

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
                Resumen = resumen,
                MostrarMensajeBusqueda = false
            };

            return View(viewModel);
        }

        // GET: Ejemplares/Details/5
        public async Task<IActionResult> Details(int? id)
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

            // Obtener historial de préstamos de este ejemplar
            var historialPrestamos = await _context.Prestamos
                .Include(p => p.Usuario)
                .Where(p => p.EjemplarId == id)
                .OrderByDescending(p => p.FechaPrestamo)
                .Take(10)
                .ToListAsync();

            ViewBag.HistorialPrestamos = historialPrestamos;

            return View(ejemplar);
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
        public async Task<IActionResult> Create([Bind("LibroId,CodigoBarras,Estado,Notas,Ubicacion")] Ejemplar ejemplar)
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
            else if (Ean13Helper.TryNormalize(ejemplar.CodigoBarras, out var codigoNormalizado, out var error))
            {
                var codigoExistente = await _context.Ejemplares
                    .AnyAsync(e => e.CodigoBarras == codigoNormalizado);
                
                if (codigoExistente)
                {
                    ModelState.AddModelError("CodigoBarras", "Ya existe un ejemplar con este código de barras.");
                }
                else
                {
                    ejemplar.CodigoBarras = codigoNormalizado;
                }
            }
            else
            {
                ModelState.AddModelError("CodigoBarras", error);
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
                    TempData["CodigosAImprimir"] = ejemplar.Id.ToString();
                    TempData["AutoPrintCodigos"] = true;
                    
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
            var nuevosEjemplares = new List<Ejemplar>();
            var prefijoNumerico = string.Concat((prefijoCodigo ?? string.Empty).Where(char.IsDigit));
            var usarPrefijo = !string.IsNullOrWhiteSpace(prefijoNumerico);

            for (int i = 0; i < cantidad; i++)
            {
                try
                {
                    string codigoBarras;

                    if (usarPrefijo)
                    {
                        var baseInput = $"{prefijoNumerico}{(i + 1):D6}";
                        if (!Ean13Helper.TryNormalize(baseInput, out codigoBarras, out var errorPrefijo))
                        {
                            errores.Add($"Prefijo inválido para el ejemplar {i + 1}: {errorPrefijo}");
                            continue;
                        }
                    }
                    else
                    {
                        codigoBarras = await GenerarCodigoBarrasUnico();
                    }

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
                    nuevosEjemplares.Add(ejemplar);
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
                
                if (nuevosEjemplares.Any())
                {
                    TempData["CodigosAImprimir"] = string.Join(",", nuevosEjemplares.Select(e => e.Id));
                    TempData["AutoPrintCodigos"] = true;
                }
                
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,LibroId,CodigoBarras,Estado,Notas,Ubicacion")] Ejemplar ejemplar)
        {
            if (id != ejemplar.Id)
            {
                return NotFound();
            }

            // Limpiar espacios en blanco del código de barras
            ejemplar.CodigoBarras = ejemplar.CodigoBarras?.Trim() ?? string.Empty;
            
            if (Ean13Helper.TryNormalize(ejemplar.CodigoBarras, out var codigoNormalizado, out var errorEdicion))
            {
                // Validar que sea único (excepto el actual)
                var codigoExistente = await _context.Ejemplares
                    .AnyAsync(e => e.CodigoBarras == codigoNormalizado && e.Id != ejemplar.Id);
                
                if (codigoExistente)
                {
                    ModelState.AddModelError("CodigoBarras", "Ya existe otro ejemplar con este código de barras.");
                }
                else
                {
                    ejemplar.CodigoBarras = codigoNormalizado;
                }
            }
            else
            {
                var mensaje = string.IsNullOrWhiteSpace(ejemplar.CodigoBarras)
                    ? "El código de barras es obligatorio."
                    : errorEdicion;
                ModelState.AddModelError("CodigoBarras", mensaje);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ejemplarOriginal = await _context.Ejemplares
                        .Include(e => e.Libro)
                        .FirstOrDefaultAsync(e => e.Id == id);
                    if (ejemplarOriginal == null)
                    {
                        return NotFound();
                    }

                    var estadoAnterior = ejemplarOriginal.Estado;
                    var nuevoEstado = ejemplar.Estado;

                    if (!string.Equals(estadoAnterior, nuevoEstado, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(nuevoEstado, "Disponible", StringComparison.OrdinalIgnoreCase))
                        {
                            var prestamoActivo = await _context.Prestamos
                                .Include(p => p.Usuario)
                                .FirstOrDefaultAsync(p => p.EjemplarId == ejemplarOriginal.Id && p.Estado == "Activo");

                            if (prestamoActivo != null)
                            {
                                prestamoActivo.Estado = "Devuelto";
                                prestamoActivo.FechaDevolucion = DateTime.Now;
                                _context.Entry(prestamoActivo).State = EntityState.Modified;

                                if (prestamoActivo.Usuario != null && prestamoActivo.Usuario.PrestamosActivos > 0)
                                {
                                    prestamoActivo.Usuario.PrestamosActivos--;
                                    _context.Entry(prestamoActivo.Usuario).State = EntityState.Modified;
                                }
                            }

                            ejemplarOriginal.PrestadoA = null;
                            ejemplarOriginal.FechaPrestamo = null;
                        }
                        else if (string.Equals(nuevoEstado, "Prestado", StringComparison.OrdinalIgnoreCase))
                        {
                            var prestamoActivo = await _context.Prestamos
                                .Include(p => p.Usuario)
                                .Where(p => p.EjemplarId == ejemplarOriginal.Id && p.Estado == "Activo")
                                .OrderByDescending(p => p.FechaPrestamo)
                                .FirstOrDefaultAsync();

                            if (prestamoActivo?.Usuario != null)
                            {
                                ejemplarOriginal.PrestadoA = prestamoActivo.Usuario.NombreCompleto;
                                ejemplarOriginal.FechaPrestamo = prestamoActivo.FechaPrestamo;
                            }
                        }
                        else
                        {
                            ejemplarOriginal.PrestadoA = null;
                            ejemplarOriginal.FechaPrestamo = null;
                        }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImprimirSeleccionados([FromForm] List<int>? seleccionados, int? libroId, bool autoPrint = false)
        {
            if (seleccionados == null || !seleccionados.Any())
            {
                TempData["WarningMessage"] = "Selecciona al menos un ejemplar para imprimir.";
                return RedirectToAction(nameof(Index), new { libroId });
            }

            var ejemplares = await _context.Ejemplares
                .Include(e => e.Libro)
                .Where(e => seleccionados.Contains(e.Id))
                .OrderBy(e => e.Libro != null ? e.Libro.Titulo : string.Empty)
                .ThenBy(e => e.CodigoBarras)
                .ToListAsync();

            if (!ejemplares.Any())
            {
                TempData["WarningMessage"] = "No se encontraron ejemplares válidos para imprimir.";
                return RedirectToAction(nameof(Index), new { libroId });
            }

            var titulo = libroId.HasValue && ejemplares.FirstOrDefault()?.Libro != null
                ? $"Códigos de barras - {ejemplares.First().Libro!.Titulo}"
                : "Códigos de barras de ejemplares";

            var viewModel = new EjemplaresImprimirViewModel
            {
                Ejemplares = ejemplares,
                LibroId = libroId,
                TituloDocumento = titulo,
                AutoPrint = autoPrint
            };

            return View("Imprimir", viewModel);
        }

        private bool EjemplarExists(int id)
        {
            return _context.Ejemplares.Any(e => e.Id == id);
        }

        private async Task MigrarCodigosBarrasAsync()
        {
            if (_codigosMigrados)
            {
                return;
            }

            var todosLosEjemplares = await _context.Ejemplares.ToListAsync();
            var ejemplaresInvalidos = todosLosEjemplares
                .Where(e => !Ean13Helper.EsCodigoValido(e.CodigoBarras ?? string.Empty))
                .ToList();

            if (ejemplaresInvalidos.Any())
            {
                var codigosReservados = new HashSet<string>(todosLosEjemplares.Select(e => e.CodigoBarras ?? string.Empty), StringComparer.Ordinal);

                foreach (var ejemplar in ejemplaresInvalidos)
                {
                    string nuevoCodigo;
                    do
                    {
                        nuevoCodigo = await GenerarCodigoBarrasUnico();
                    } while (!codigosReservados.Add(nuevoCodigo));

                    ejemplar.CodigoBarras = nuevoCodigo;
                }

                await _context.SaveChangesAsync();
            }

            _codigosMigrados = true;
        }

        private async Task<string> GenerarCodigoBarrasUnico()
        {
            return await Ean13Helper.GenerarCodigoUnicoAsync(async codigo =>
                await _context.Ejemplares.AnyAsync(e => e.CodigoBarras == codigo));
        }
    }
}

