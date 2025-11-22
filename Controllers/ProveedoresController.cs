using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(string? searchString, string? tipo)
        {
            var proveedores = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                proveedores = proveedores.Where(p => p.Nombre.Contains(searchString) || 
                                                   p.Contacto!.Contains(searchString) ||
                                                   p.Email!.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                proveedores = proveedores.Where(p => p.Tipo == tipo);
            }

            ViewBag.SearchString = searchString;
            ViewBag.TipoSeleccionado = tipo;

            return View(await proveedores.OrderBy(p => p.Nombre).ToListAsync());
        }

        // GET: Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
            {
                return NotFound();
            }

            // Cargar libros del proveedor
            var libros = await _context.Libros
                .Where(l => l.ProveedorId == id)
                .OrderBy(l => l.Titulo)
                .ToListAsync();

            ViewBag.Libros = libros;

            return View(proveedor);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Contacto,Email,Telefono,Tipo,Notas")] Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    proveedor.FechaRegistro = DateTime.Now;
                    _context.Add(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proveedor agregado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al guardar el proveedor: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el proveedor: {ex.Message}";
                }
            }

            return View(proveedor);
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Contacto,Email,Telefono,Tipo,Notas,LibrosProporcionados,FechaRegistro")] Proveedor proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proveedor actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ProveedorExists(proveedor.Id))
                    {
                        TempData["ErrorMessage"] = "El proveedor fue eliminado por otro usuario.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "El proveedor fue modificado por otro usuario. Por favor, recarga la página e intenta de nuevo.";
                        return View(proveedor);
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al guardar el proveedor: {ex.InnerException?.Message ?? ex.Message}";
                    return View(proveedor);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al guardar el proveedor: {ex.Message}";
                    return View(proveedor);
                }
                return RedirectToAction(nameof(Index));
            }

            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                try
                {
                    // Verificar si tiene libros asociados
                    var libros = await _context.Libros
                        .Where(l => l.ProveedorId == id)
                        .Select(l => l.Titulo)
                        .ToListAsync();

                    if (libros.Any())
                    {
                        var titulos = string.Join(", ", libros.Take(5));
                        var mensaje = libros.Count > 5 
                            ? $"No se puede eliminar el proveedor porque tiene {libros.Count} libros asociados (ej: {titulos}...). Primero debe eliminar o cambiar el proveedor de estos libros."
                            : $"No se puede eliminar el proveedor porque tiene los siguientes libros asociados: {titulos}. Primero debe eliminar o cambiar el proveedor de estos libros.";
                        TempData["ErrorMessage"] = mensaje;
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Proveedores.Remove(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proveedor eliminado correctamente.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Error al eliminar el proveedor: {ex.InnerException?.Message ?? ex.Message}. Puede que tenga relaciones con otros datos.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error inesperado al eliminar el proveedor: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}
