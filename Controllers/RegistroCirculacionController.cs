using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class RegistroCirculacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistroCirculacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RegistroCirculacion
        public async Task<IActionResult> Index(string? estado, string? searchString, DateTime? fechaInicio, DateTime? fechaFin, string? rango)
        {
            var prestamos = _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .AsQueryable();

            var rangoSeleccionado = string.IsNullOrEmpty(rango) ? "Hoy" : rango;
            var inicio = fechaInicio?.Date;
            var fin = fechaFin?.Date;

            if (!inicio.HasValue || !fin.HasValue)
            {
                switch (rangoSeleccionado)
                {
                    case "Semana":
                        fin = DateTime.Today;
                        inicio = DateTime.Today.AddDays(-6);
                        break;
                    case "Mes":
                        fin = DateTime.Today;
                        inicio = DateTime.Today.AddDays(-29);
                        break;
                    case "MesActual":
                        inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        fin = inicio.Value.AddMonths(1).AddDays(-1);
                        break;
                    default:
                        inicio = DateTime.Today;
                        fin = DateTime.Today;
                        rangoSeleccionado = "Hoy";
                        break;
                }
            }

            var fechaInicioFiltro = inicio!.Value.Date;
            var fechaFinFiltro = fin!.Value.Date;
            var fechaFinExclusiva = fechaFinFiltro.AddDays(1);

            prestamos = prestamos.Where(p => p.FechaPrestamo >= fechaInicioFiltro && p.FechaPrestamo < fechaFinExclusiva);

            // Filtro por estado
            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "Vencidos")
                {
                    prestamos = prestamos.Where(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now);
                }
                else if (estado == "PorVencer")
                {
                    prestamos = prestamos.Where(p => p.Estado == "Activo" && 
                                                   p.FechaVencimiento <= DateTime.Now.AddDays(3) && 
                                                   p.FechaVencimiento >= DateTime.Now);
                }
                else
                {
                    prestamos = prestamos.Where(p => p.Estado == estado);
                }
            }

            // Búsqueda por libro o usuario
            if (!string.IsNullOrEmpty(searchString))
            {
                prestamos = prestamos.Where(p => 
                    (p.EjemplarId > 0 && p.Ejemplar != null && p.Ejemplar.Libro != null && 
                     (p.Ejemplar.Libro.Titulo.Contains(searchString) || 
                      p.Ejemplar.Libro.Autor.Contains(searchString))) ||
                    p.Usuario.Nombre.Contains(searchString) ||
                    p.Usuario.Apellido.Contains(searchString) ||
                    p.Usuario.RUT.Contains(searchString));
            }

            var lista = await prestamos
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync();

            var resumen = new RegistroCirculacionResumenViewModel
            {
                TotalPrestamos = lista.Count,
                PrestamosActivos = lista.Count(p => p.Estado == "Activo"),
                PrestamosDevueltos = lista.Count(p => p.Estado == "Devuelto"),
                PrestamosVencidos = lista.Count(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now)
            };

            var viewModel = new RegistroCirculacionViewModel
            {
                Prestamos = lista,
                FechaInicio = fechaInicioFiltro,
                FechaFin = fechaFinFiltro,
                EstadoSeleccionado = estado,
                SearchString = searchString,
                RangoSeleccionado = rangoSeleccionado,
                Resumen = resumen
            };

            return View(viewModel);
        }

        // GET: RegistroCirculacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prestamo = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prestamo == null)
            {
                return NotFound();
            }

            return View(prestamo);
        }

        // POST: RegistroCirculacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EjemplarId,LibroId,UsuarioId,FechaPrestamo,FechaVencimiento,FechaDevolucion,Estado")] Prestamo prestamo)
        {
            if (id != prestamo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validar que la fecha de préstamo no sea futura
                    if (prestamo.FechaPrestamo > DateTime.Now)
                    {
                        ModelState.AddModelError("FechaPrestamo", "La fecha de préstamo no puede ser futura.");
                        var prestamoTemp = await _context.Prestamos
                            .Include(p => p.Ejemplar)
                                .ThenInclude(e => e.Libro)
                            .Include(p => p.Usuario)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        TempData["ErrorMessage"] = "La fecha de préstamo no puede ser futura.";
                        return View(prestamoTemp ?? prestamo);
                    }

                    // Validar que la fecha de vencimiento sea posterior a la fecha de préstamo
                    if (prestamo.FechaVencimiento < prestamo.FechaPrestamo)
                    {
                        ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser anterior a la fecha de préstamo.");
                        var prestamoTemp = await _context.Prestamos
                            .Include(p => p.Ejemplar)
                                .ThenInclude(e => e.Libro)
                            .Include(p => p.Usuario)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        TempData["ErrorMessage"] = "La fecha de vencimiento no puede ser anterior a la fecha de préstamo.";
                        return View(prestamoTemp ?? prestamo);
                    }

                    // Validar que la fecha de devolución no sea anterior a la fecha de préstamo
                    if (prestamo.FechaDevolucion.HasValue && prestamo.FechaDevolucion.Value < prestamo.FechaPrestamo)
                    {
                        ModelState.AddModelError("FechaDevolucion", "La fecha de devolución no puede ser anterior a la fecha de préstamo.");
                        var prestamoTemp = await _context.Prestamos
                            .Include(p => p.Ejemplar)
                                .ThenInclude(e => e.Libro)
                            .Include(p => p.Usuario)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        TempData["ErrorMessage"] = "La fecha de devolución no puede ser anterior a la fecha de préstamo.";
                        return View(prestamoTemp ?? prestamo);
                    }

                    // Si se cambia el estado a Devuelto, actualizar el ejemplar
                    var prestamoOriginal = await _context.Prestamos
                        .Include(p => p.Ejemplar)
                        .Include(p => p.Usuario)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (prestamoOriginal != null)
                    {
                        // Si cambió de Activo a Devuelto
                        if (prestamoOriginal.Estado == "Activo" && prestamo.Estado == "Devuelto")
                        {
                            if (prestamoOriginal.EjemplarId > 0 && prestamoOriginal.Ejemplar != null)
                            {
                                var ejemplar = prestamoOriginal.Ejemplar;
                                ejemplar.Estado = "Disponible";
                                ejemplar.PrestadoA = null;
                                ejemplar.FechaPrestamo = null;
                                _context.Entry(ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            }

                            var usuario = prestamoOriginal.Usuario;
                            if (usuario.PrestamosActivos > 0)
                            {
                                usuario.PrestamosActivos--;
                            }
                            _context.Entry(usuario).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                            prestamo.FechaDevolucion = prestamo.FechaDevolucion ?? DateTime.Now;
                        }
                        // Si cambió de Devuelto a Activo (caso poco común pero posible)
                        else if (prestamoOriginal.Estado == "Devuelto" && prestamo.Estado == "Activo")
                        {
                            if (prestamoOriginal.EjemplarId > 0 && prestamoOriginal.Ejemplar != null)
                            {
                                var ejemplar = prestamoOriginal.Ejemplar;
                                ejemplar.Estado = "Prestado";
                                ejemplar.PrestadoA = prestamoOriginal.Usuario?.NombreCompleto ?? "Usuario desconocido";
                                ejemplar.FechaPrestamo = prestamo.FechaPrestamo;
                                _context.Entry(ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            }

                            var usuario = prestamoOriginal.Usuario;
                            usuario.PrestamosActivos++;
                            _context.Entry(usuario).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                            prestamo.FechaDevolucion = null;
                        }
                    }

                    _context.Entry(prestamo).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Registro de circulación actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrestamoExists(prestamo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var prestamoError = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);
            return View(prestamoError ?? prestamo);
        }

        // GET: RegistroCirculacion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prestamo = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prestamo == null)
            {
                return NotFound();
            }

            return View(prestamo);
        }

        private bool PrestamoExists(int id)
        {
            return _context.Prestamos.Any(e => e.Id == id);
        }
    }
}

