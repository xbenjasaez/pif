using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Utils;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] CursosChile =
        {
            "Prekínder",
            "Kínder",
            "1° Básico",
            "2° Básico",
            "3° Básico",
            "4° Básico",
            "5° Básico",
            "6° Básico",
            "7° Básico",
            "8° Básico",
            "1° Medio",
            "2° Medio",
            "3° Medio",
            "4° Medio"
        };

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index(string? searchString, string? estado)
        {
            var usuarios = _context.Usuarios.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usuarios = usuarios.Where(u => u.Nombre.Contains(searchString) || 
                                             u.Apellido.Contains(searchString) || 
                                             u.RUT.Contains(searchString) ||
                                             u.Email!.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                usuarios = usuarios.Where(u => u.Estado == estado);
            }

            ViewBag.SearchString = searchString;
            ViewBag.EstadoSeleccionado = estado;

            return View(await usuarios.OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToListAsync());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Cargar préstamos del usuario
            var prestamos = await _context.Prestamos
                .Where(p => p.UsuarioId == id)
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .OrderByDescending(p => p.FechaPrestamo)
                .Take(10)
                .ToListAsync();

            ViewBag.Prestamos = prestamos;

            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            ViewBag.Cursos = ObtenerCursos();
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellido,RUT,Email,Telefono,Estado,Notas,Curso")] Usuario usuario)
        {
            await ValidarRutAsync(usuario);

            if (ModelState.IsValid)
            {
                try
                {
                    usuario.FechaRegistro = DateTime.Now;
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuario agregado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message?.Contains("UNIQUE") == true || ex.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("RUT", "Ya existe un usuario con este RUT.");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Error al guardar el usuario: {ex.InnerException?.Message ?? ex.Message}";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el usuario: {ex.Message}";
                }
            }

            ViewBag.Cursos = ObtenerCursos(usuario.Curso);
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Cursos = ObtenerCursos(usuario.Curso);
            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,RUT,Email,Telefono,Estado,Notas,PrestamosActivos,PrestamosVencidos,FechaRegistro,Curso")] Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            await ValidarRutAsync(usuario, id);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!UsuarioExists(usuario.Id))
                    {
                        TempData["ErrorMessage"] = "El usuario fue eliminado por otro usuario.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "El usuario fue modificado por otro usuario. Por favor, recarga la página e intenta de nuevo.";
                        return View(usuario);
                    }
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message?.Contains("UNIQUE") == true || ex.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("RUT", "Ya existe otro usuario con este RUT.");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Error al guardar el usuario: {ex.InnerException?.Message ?? ex.Message}";
                    }
                    ViewBag.Cursos = ObtenerCursos(usuario.Curso);
                    return View(usuario);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el usuario: {ex.Message}";
                    ViewBag.Cursos = ObtenerCursos(usuario.Curso);
                    return View(usuario);
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Cursos = ObtenerCursos(usuario.Curso);
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Verificar préstamos activos para mostrar advertencia
            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Where(p => p.UsuarioId == id && p.Estado == "Activo")
                .ToListAsync();

            if (prestamosActivos.Any())
            {
                ViewBag.PrestamosActivos = prestamosActivos;
                TempData["WarningMessage"] = $"⚠️ Este usuario tiene {prestamosActivos.Count} préstamo(s) activo(s). Debe devolverlos primero antes de eliminar el usuario.";
            }

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                try
                {
                    // Verificar si tiene préstamos activos
                    var prestamosActivos = await _context.Prestamos
                        .Include(p => p.Ejemplar)
                            .ThenInclude(e => e.Libro)
                        .Where(p => p.UsuarioId == id && p.Estado == "Activo")
                        .ToListAsync();

                    if (prestamosActivos.Any())
                    {
                        var libros = string.Join(", ", prestamosActivos
                            .Where(p => p.EjemplarId > 0 && p.Ejemplar != null && p.Ejemplar.Libro != null)
                            .Select(p => p.Ejemplar!.Libro!.Titulo));
                        TempData["ErrorMessage"] = $"No se puede eliminar el usuario porque tiene préstamos activos de los siguientes libros: {libros}. Debe devolver los préstamos primero desde el apartado 'Préstamos' o 'Registro de Circulación'.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Verificar si tiene préstamos históricos
                    var prestamosHistoricos = await _context.Prestamos
                        .AnyAsync(p => p.UsuarioId == id);

                    if (prestamosHistoricos)
                    {
                        TempData["WarningMessage"] = "Este usuario tiene un historial de préstamos. Se eliminará el usuario pero se mantendrá el registro de préstamos para auditoría.";
                    }

                    _context.Usuarios.Remove(usuario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al eliminar el usuario: {ex.InnerException?.Message ?? ex.Message}. Puede que tenga relaciones con otros datos.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al eliminar el usuario: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }

        private IEnumerable<SelectListItem> ObtenerCursos(string? seleccionado = null)
        {
            return CursosChile
                .Select(c => new SelectListItem
                {
                    Value = c,
                    Text = c,
                    Selected = c == seleccionado
                })
                .ToList();
        }

        private async Task ValidarRutAsync(Usuario usuario, int? ignorarId = null)
        {
            if (!RutValidator.ValidarRUT(usuario.RUT))
            {
                ModelState.AddModelError("RUT", "El RUT ingresado no es válido.");
                return;
            }

            usuario.RUT = RutValidator.FormatearRUT(usuario.RUT);

            var existe = await _context.Usuarios.AnyAsync(u =>
                u.RUT == usuario.RUT && (!ignorarId.HasValue || u.Id != ignorarId.Value));

            if (existe)
            {
                ModelState.AddModelError("RUT", "Ya existe un usuario con este RUT.");
            }
        }
    }
}
