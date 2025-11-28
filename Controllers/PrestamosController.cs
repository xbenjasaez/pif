using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Services;
using Microsoft.AspNetCore.Authorization;
using BibliotecaVirtualWeb.Helpers;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario,Asistente")]
    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditoriaService _auditoria;
        private readonly IAlertaSistemaService _alertas;

        public PrestamosController(ApplicationDbContext context, IAuditoriaService auditoria, IAlertaSistemaService alertas)
        {
            _context = context;
            _auditoria = auditoria;
            _alertas = alertas;
        }

        // GET: Prestamos
        [Authorize(Roles = "Admin,Bibliotecario")]
        public async Task<IActionResult> Index(string? estado, string? searchString)
        {
            var prestamos = _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Libro)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                prestamos = prestamos.Where(p =>
                    (p.Ejemplar != null && p.Ejemplar.CodigoBarras.Contains(term)) ||
                    (p.Ejemplar != null && p.Ejemplar.Libro != null &&
                        (p.Ejemplar.Libro.Titulo.Contains(term) || p.Ejemplar.Libro.Autor.Contains(term))) ||
                    (p.Libro != null && (p.Libro.Titulo.Contains(term) || p.Libro.Autor.Contains(term))) ||
                    (p.Usuario != null &&
                        (p.Usuario.Nombre.Contains(term) ||
                         p.Usuario.Apellido.Contains(term) ||
                         p.Usuario.RUT.Contains(term) ||
                         (p.Usuario.Curso ?? "").Contains(term))));
            }

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

            var lista = await prestamos
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync();

            var resumen = new PrestamosResumenViewModel
            {
                Total = lista.Count,
                Activos = lista.Count(p => p.Estado == "Activo"),
                Devueltos = lista.Count(p => p.Estado == "Devuelto"),
                Vencidos = lista.Count(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now)
            };

            var viewModel = new PrestamosIndexViewModel
            {
                Prestamos = lista,
                EstadoSeleccionado = estado,
                SearchString = searchString,
                Resumen = resumen
            };

            return View(viewModel);
        }

        // GET: Prestamos/Details/5
        [Authorize(Roles = "Admin,Bibliotecario")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prestamo == null)
            {
                return NotFound();
            }

            return View(prestamo);
        }

        // GET: Prestamos/Create
        [Authorize(Roles = "Admin,Bibliotecario")]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        // GET: Prestamos/DevolucionRapida
        public async Task<IActionResult> DevolucionRapida()
        {
            var inicioDia = DateTime.Today;
            var finDia = inicioDia.AddDays(1);

            var devolucionesHoy = await _context.Prestamos
                .CountAsync(p => p.FechaDevolucion >= inicioDia && p.FechaDevolucion < finDia);

            var viewModel = new DevolucionRapidaViewModel
            {
                DevolucionesHoy = devolucionesHoy,
                DevolucionesRecientes = new List<DevolucionRecienteViewModel>()
            };

            return View(viewModel);
        }

        // GET: Prestamos/PrestamoRapido
        public IActionResult PrestamoRapido()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PrestamosHoyCount()
        {
            var inicioDia = DateTime.Today;
            var finDia = inicioDia.AddDays(1);

            var totalHoy = await _context.Prestamos
                .CountAsync(p => p.FechaPrestamo >= inicioDia && p.FechaPrestamo < finDia);

            return Json(new { success = true, count = totalHoy });
        }

        // POST: Prestamos/CrearPrestamoRapido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPrestamoRapido([FromBody] PrestamoRapidoRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CodigoBarras) || string.IsNullOrWhiteSpace(request.RutUsuario))
            {
                return Json(new { success = false, message = "Debes proporcionar el código del ejemplar y el RUT del usuario." });
            }

            if (!TryNormalizarCodigo(request.CodigoBarras, out var codigo, out var errorCodigo))
            {
                return Json(new { success = false, message = errorCodigo });
            }
            var rutUsuario = request.RutUsuario.Trim();
            var rutNormalizado = NormalizarRut(rutUsuario);

            // Buscar ejemplar
            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.CodigoBarras == codigo);

            if (ejemplar == null)
            {
                return Json(new { success = false, message = "No encontramos ningún ejemplar con ese código de barras." });
            }

            if (ejemplar.Estado != "Disponible")
            {
                var estadosNoPrestables = new[] { "Dado de baja", "Extraviado" };
                var mensajeError = estadosNoPrestables.Contains(ejemplar.Estado)
                    ? $"El ejemplar está marcado como '{ejemplar.Estado}' y no puede ser prestado."
                    : $"El ejemplar no está disponible. Estado actual: {ejemplar.Estado}.";
                return Json(new { success = false, message = mensajeError });
            }

            // Buscar usuario por RUT o ID
            Usuario? usuario = null;
            
            if (!string.IsNullOrWhiteSpace(rutNormalizado))
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.RUT != null &&
                        u.RUT.Replace(".", string.Empty)
                            .Replace("-", string.Empty)
                            .Replace(" ", string.Empty)
                            .ToUpper() == rutNormalizado);
            }
            
            // Si no se encuentra por RUT, intentar por ID
            if (usuario == null && int.TryParse(rutUsuario, out int usuarioId))
            {
                usuario = await _context.Usuarios.FindAsync(usuarioId);
            }

            if (usuario == null)
            {
                return Json(new { success = false, message = $"No encontramos ningún usuario con RUT o ID: {rutUsuario}." });
            }

            // Verificar si el usuario tiene préstamos vencidos
            var tieneVencidos = await _context.Prestamos
                .AnyAsync(p => p.UsuarioId == usuario.Id 
                    && p.Estado == "Activo" 
                    && p.FechaVencimiento < DateTime.Now);

            if (tieneVencidos)
            {
                return Json(new { success = false, message = $"El usuario {usuario.Nombre} tiene préstamos vencidos. Debe devolverlos antes de solicitar nuevos préstamos." });
            }

            // Crear el préstamo
            var prestamo = new Prestamo
            {
                EjemplarId = ejemplar.Id,
                LibroId = ejemplar.Libro?.Id ?? ejemplar.LibroId,
                UsuarioId = usuario.Id,
                FechaPrestamo = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(14), // 14 días por defecto
                Estado = "Activo"
            };

            try
            {
                _context.Prestamos.Add(prestamo);
                
                // Actualizar estado del ejemplar
                ejemplar.Estado = "Prestado";
                ejemplar.PrestadoA = usuario.NombreCompleto;
                ejemplar.FechaPrestamo = prestamo.FechaPrestamo;
                _context.Update(ejemplar);

                usuario.PrestamosActivos++;
                _context.Update(usuario);

                await _context.SaveChangesAsync();
                await _auditoria.RegistrarAsync(
                    "Préstamo rápido",
                    $"Se prestó '{ejemplar.Libro?.Titulo ?? "Sin título"}' al usuario {usuario.Nombre} ({usuario.RUT})",
                    User);

                return Json(new
                {
                    success = true,
                    message = $"✅ Préstamo creado exitosamente para {usuario.Nombre}",
                    data = new
                    {
                        libro = ejemplar.Libro?.Titulo ?? "Sin título",
                        codigoBarras = ejemplar.CodigoBarras,
                        usuario = usuario.Nombre,
                        rut = usuario.RUT,
                        curso = usuario.Curso,
                        fechaPrestamo = prestamo.FechaPrestamo,
                        fechaVencimiento = prestamo.FechaVencimiento
                    }
                });
            }
            catch (Exception ex)
            {
                await _alertas.RegistrarErrorAsync(
                    "Error al crear préstamo rápido",
                    ex.Message,
                    ex.ToString());

                return Json(new { success = false, message = $"Error al crear el préstamo: {ex.Message}" });
            }
        }

        // GET: Prestamos/BuscarEjemplar?codigoBarras=XXX
        [HttpGet]
        public async Task<IActionResult> BuscarEjemplar(string codigoBarras)
        {
            if (!TryNormalizarCodigo(codigoBarras, out var codigoNormalizado, out var errorCodigo))
            {
                return Json(new { success = false, message = errorCodigo });
            }

            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.CodigoBarras == codigoNormalizado);

            if (ejemplar == null)
            {
                return Json(new { success = false, message = "Ejemplar no encontrado. Verifica el código de barras." });
            }

            if (ejemplar.Estado != "Disponible")
            {
                var estadosNoPrestables = new[] { "Dado de baja", "Extraviado" };
                var mensaje = estadosNoPrestables.Contains(ejemplar.Estado)
                    ? $"Este ejemplar está marcado como '{ejemplar.Estado}' y no se puede prestar."
                    : $"El ejemplar no está disponible. Estado actual: {ejemplar.Estado}.";

                return Json(new { 
                    success = false, 
                    message = mensaje,
                    ejemplar = new { 
                        id = ejemplar.Id, 
                        codigoBarras = ejemplar.CodigoBarras,
                        estado = ejemplar.Estado,
                        libro = new {
                            id = ejemplar.Libro?.Id ?? ejemplar.LibroId,
                            titulo = ejemplar.Libro?.Titulo ?? "Sin título",
                            autor = ejemplar.Libro?.Autor ?? "Autor no disponible"
                        }
                    }
                });
            }

            return Json(new { 
                success = true,
                ejemplar = new { 
                    id = ejemplar.Id, 
                    codigoBarras = ejemplar.CodigoBarras,
                    estado = ejemplar.Estado,
                    libro = new {
                        id = ejemplar.Libro?.Id ?? ejemplar.LibroId,
                        titulo = ejemplar.Libro?.Titulo ?? "Sin título",
                        autor = ejemplar.Libro?.Autor ?? "Autor no disponible",
                        isbn = ejemplar.Libro?.ISBN ?? string.Empty
                    }
                }
            });
        }

        // POST: Prestamos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bibliotecario")]
        public async Task<IActionResult> Create(int EjemplarId, int UsuarioId)
        {
            // Log para debugging
            System.Diagnostics.Debug.WriteLine($"Create POST - EjemplarId: {EjemplarId}, UsuarioId: {UsuarioId}");
            
            // Limpiar todos los errores de ModelState para evitar validaciones automáticas
            ModelState.Clear();
            
            // Validar manualmente que los campos requeridos estén presentes
            if (EjemplarId == 0)
            {
                ModelState.AddModelError("EjemplarId", "Debe escanear un código de barras de un ejemplar.");
            }
            
            if (UsuarioId == 0)
            {
                ModelState.AddModelError("UsuarioId", "Debe seleccionar un usuario.");
            }

            // Si hay errores de validación, mostrarlos y retornar
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                System.Diagnostics.Debug.WriteLine($"ModelState inválido: {string.Join(", ", errors)}");
                TempData["ErrorMessage"] = $"Error de validación: {string.Join(", ", errors)}";
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }
            
            // Obtener el ejemplar y validar que esté disponible
            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == EjemplarId);

            if (ejemplar == null)
            {
                ModelState.AddModelError("EjemplarId", "El ejemplar no fue encontrado.");
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }

            if (ejemplar.Estado != "Disponible")
            {
                var estadosNoPrestables = new[] { "Dado de baja", "Extraviado" };
                var mensajeError = estadosNoPrestables.Contains(ejemplar.Estado)
                    ? $"Este ejemplar está marcado como '{ejemplar.Estado}' y no puede ser prestado."
                    : $"El ejemplar no está disponible. Estado actual: {ejemplar.Estado}.";
                ModelState.AddModelError("EjemplarId", mensajeError);
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }

            // Crear el objeto Prestamo
            var prestamo = new Prestamo
            {
                EjemplarId = EjemplarId,
                LibroId = ejemplar.LibroId,
                UsuarioId = UsuarioId
            };

            // Validar que el usuario esté activo
            var usuario = await _context.Usuarios.FindAsync(prestamo.UsuarioId);
            if (usuario == null || usuario.Estado != "Activo")
            {
                ModelState.AddModelError("UsuarioId", "El usuario seleccionado no está activo.");
                return View(prestamo);
            }

            // Verificar si el usuario tiene préstamos vencidos
            var prestamosVencidos = await _context.Prestamos
                .AnyAsync(p => p.UsuarioId == prestamo.UsuarioId && 
                             p.Estado == "Activo" && 
                             p.FechaVencimiento < DateTime.Now);

            if (prestamosVencidos)
            {
                ModelState.AddModelError("UsuarioId", "El usuario tiene préstamos vencidos. Debe devolverlos antes de solicitar nuevos libros.");
                return View(prestamo);
            }

            prestamo.FechaPrestamo = DateTime.Now;
            prestamo.FechaVencimiento = DateTime.Now.AddDays(15); // 15 días por defecto
            prestamo.Estado = "Activo";

            // Agregar el préstamo
            _context.Prestamos.Add(prestamo);

            // Actualizar estado del ejemplar
            ejemplar.Estado = "Prestado";
            ejemplar.PrestadoA = usuario.NombreCompleto;
            ejemplar.FechaPrestamo = prestamo.FechaPrestamo;
            _context.Entry(ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            // Actualizar contador de préstamos del usuario
            usuario.PrestamosActivos++;
            _context.Entry(usuario).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Intentando guardar préstamo - EjemplarId: {prestamo.EjemplarId}, UsuarioId: {prestamo.UsuarioId}");
                
                // Verificación final de concurrencia antes de guardar - usar AsNoTracking para obtener datos frescos
                var ejemplarVerificacion = await _context.Ejemplares
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == prestamo.EjemplarId);
                    
                if (ejemplarVerificacion == null)
                {
                    TempData["ErrorMessage"] = $"El ejemplar seleccionado ya no existe. Puede que haya sido eliminado.";
                    return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
                }
                
                if (ejemplarVerificacion.Estado != "Disponible")
                {
                    // Verificar si realmente hay un préstamo activo o si es inconsistencia
                    var prestamoActivoReal = await _context.Prestamos
                        .AnyAsync(p => p.EjemplarId == prestamo.EjemplarId && p.Estado == "Activo");
                    
                    if (!prestamoActivoReal && ejemplarVerificacion.Estado == "Prestado")
                    {
                        // Hay inconsistencia: el ejemplar está marcado como prestado pero no hay préstamo activo
                        // Corregir el estado automáticamente y guardar
                        ejemplar.Estado = "Disponible";
                        ejemplar.PrestadoA = null;
                        ejemplar.FechaPrestamo = null;
                        _context.Entry(ejemplar).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        
                        try
                        {
                            await _context.SaveChangesAsync();
                            TempData["SuccessMessage"] = $"Se detectó y corrigió una inconsistencia en el estado del ejemplar. Ahora puedes crear el préstamo.";
                        }
                        catch
                        {
                            TempData["WarningMessage"] = $"Se detectó una inconsistencia en el estado del ejemplar. Por favor, intenta de nuevo.";
                        }
                        
                        return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
                    }
                    
                    TempData["ErrorMessage"] = $"El ejemplar ya no está disponible. Estado actual: {ejemplarVerificacion.Estado}. Puede que otro usuario lo haya prestado. Por favor, intenta de nuevo.";
                    return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Préstamo guardado exitosamente");
                TempData["SuccessMessage"] = $"Préstamo creado correctamente. El libro '{ejemplar.Libro.Titulo}' (ejemplar {ejemplar.CodigoBarras}) ahora está prestado a {usuario.NombreCompleto}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de concurrencia al guardar préstamo: {ex.Message}");
                TempData["ErrorMessage"] = "El ejemplar fue modificado por otro usuario. Por favor, intenta de nuevo.";
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de base de datos al guardar préstamo: {ex.Message}");
                TempData["ErrorMessage"] = $"Error al guardar el préstamo en la base de datos: {ex.InnerException?.Message ?? ex.Message}";
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inesperado al guardar préstamo: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", $"Error inesperado al guardar el préstamo: {ex.Message}");
                TempData["ErrorMessage"] = $"Error inesperado al guardar el préstamo: {ex.Message}";
                return View(new Prestamo { EjemplarId = EjemplarId, UsuarioId = UsuarioId });
            }
        }

        // POST: Prestamos/Devolver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bibliotecario")]
        public async Task<IActionResult> Devolver(int id)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null)
            {
                return NotFound();
            }

            if (prestamo.Estado != "Activo")
            {
                TempData["ErrorMessage"] = "Este préstamo ya ha sido devuelto.";
                return RedirectToAction(nameof(Index));
            }

            var ejemplar = prestamo.Ejemplar ?? await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == prestamo.EjemplarId);

            if (ejemplar == null)
            {
                TempData["ErrorMessage"] = "No se encontró el ejemplar asociado a este préstamo.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = prestamo.Usuario ?? await _context.Usuarios.FindAsync(prestamo.UsuarioId);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "No se encontró el usuario asociado a este préstamo.";
                return RedirectToAction(nameof(Index));
            }

            PrepararEntidadesParaDevolucion(prestamo, ejemplar, usuario);

            try
            {
                await _context.SaveChangesAsync();
                var tituloLibro = ejemplar.Libro?.Titulo ?? prestamo.Libro?.Titulo ?? "Libro";
                TempData["SuccessMessage"] = $"Libro '{tituloLibro}' devuelto correctamente. Ahora está disponible.";
                await _auditoria.RegistrarAsync(
                    "Devolución manual",
                    $"Se devolvió '{tituloLibro}' del usuario {usuario.NombreCompleto} ({usuario.RUT})",
                    User);
            }
            catch (Exception ex)
            {
                await _alertas.RegistrarErrorAsync(
                    "Error en devolución manual",
                    ex.Message,
                    ex.ToString());
                TempData["ErrorMessage"] = $"Error al devolver el libro: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Prestamos/DevolverPorCodigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevolverPorCodigo([FromBody] DevolucionPorCodigoRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CodigoBarras))
            {
                return Json(new { success = false, message = "Debes escanear un código de barras válido." });
            }

            if (!TryNormalizarCodigo(request.CodigoBarras, out var codigo, out var errorCodigo))
            {
                return Json(new { success = false, message = errorCodigo });
            }

            var ejemplar = await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.CodigoBarras == codigo);

            if (ejemplar == null)
            {
                return Json(new { success = false, message = "No encontramos ningún ejemplar con ese código de barras." });
            }

            var prestamo = await _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .FirstOrDefaultAsync(p => p.EjemplarId == ejemplar.Id && p.Estado == "Activo");

            if (prestamo == null)
            {
                if (ejemplar.Estado == "Disponible")
                {
                    return Json(new { success = false, message = "Este ejemplar ya está disponible. No hay préstamos activos por devolver." });
                }

                return Json(new
                {
                    success = false,
                    message = $"El ejemplar está en estado '{ejemplar.Estado}' pero no encontramos un préstamo activo asociado. Revísalo en el Registro de Circulación."
                });
            }

            var usuario = prestamo.Usuario ?? await _context.Usuarios.FindAsync(prestamo.UsuarioId);
            if (usuario == null)
            {
                return Json(new { success = false, message = "No se encontró el usuario asociado al préstamo." });
            }

            PrepararEntidadesParaDevolucion(prestamo, ejemplar, usuario);

            try
            {
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    "Devolución rápida",
                    $"Se devolvió '{ejemplar.Libro?.Titulo ?? "Sin título"}' del usuario {usuario.NombreCompleto} ({usuario.RUT})",
                    User);

                var ahora = DateTime.Now;
                var diasRetraso = 0;
                if (prestamo.FechaVencimiento < ahora)
                {
                    diasRetraso = (int)Math.Ceiling((ahora - prestamo.FechaVencimiento).TotalDays);
                }

                return Json(new
                {
                    success = true,
                    message = $"Libro '{ejemplar.Libro?.Titulo ?? "Sin título"}' devuelto correctamente.",
                    data = new
                    {
                        prestamoId = prestamo.Id,
                        usuario = new
                        {
                            id = usuario.Id,
                            nombre = usuario.NombreCompleto,
                            rut = usuario.RUT,
                            curso = usuario.Curso
                        },
                        libro = new
                        {
                            id = ejemplar.LibroId,
                            titulo = ejemplar.Libro?.Titulo,
                            autor = ejemplar.Libro?.Autor
                        },
                        codigoBarras = ejemplar.CodigoBarras,
                        fechaPrestamo = prestamo.FechaPrestamo,
                        fechaVencimiento = prestamo.FechaVencimiento,
                        fechaDevolucion = prestamo.FechaDevolucion,
                        diasRetraso
                    }
                });
            }
            catch (Exception ex)
            {
                await _alertas.RegistrarErrorAsync(
                    "Error en devolución rápida",
                    ex.Message,
                    ex.ToString());
                return Json(new { success = false, message = $"No se pudo registrar la devolución: {ex.Message}" });
            }
        }

        // GET: Prestamos/BuscarUsuarios?q=texto
        [HttpGet]
        public async Task<IActionResult> BuscarUsuarios(string q, string termino)
        {
            var query = !string.IsNullOrWhiteSpace(termino) ? termino : q;

            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                if (!string.IsNullOrWhiteSpace(termino))
                {
                    return Json(new { success = true, usuarios = Array.Empty<object>() });
                }

                return Json(new List<object>());
            }

            query = query.Trim();
            var likeQuery = $"%{query}%";

            var usuarios = await _context.Usuarios
                .Where(u => u.Estado == "Activo" &&
                    (EF.Functions.Like(u.RUT, likeQuery) ||
                     EF.Functions.Like(u.Nombre, likeQuery) ||
                     EF.Functions.Like(u.Apellido, likeQuery)))
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Take(10)
                .Select(u => new
                {
                    id = u.Id,
                    nombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                    rut = u.RUT,
                    curso = u.Curso,
                    telefono = u.Telefono,
                    email = u.Email
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(termino))
            {
                return Json(new { success = true, usuarios });
            }

            var resultados = usuarios
                .Select(u => new
                {
                    u.id,
                    u.nombreCompleto,
                    u.rut,
                    curso = u.curso,
                    texto = string.IsNullOrWhiteSpace(u.curso)
                        ? $"{u.nombreCompleto} ({u.rut})"
                        : $"{u.nombreCompleto} ({u.rut}) - {u.curso}"
                })
                .ToList();

            return Json(resultados);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuarioPorRut(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return Json(new { success = false, message = "Ingresa un RUT o ID para buscar." });
            }

            var rutNormalizado = NormalizarRut(valor);
            Usuario? usuario = null;

            if (!string.IsNullOrWhiteSpace(rutNormalizado))
            {
                usuario = await _context.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.RUT != null &&
                        u.RUT.Replace(".", string.Empty)
                             .Replace("-", string.Empty)
                             .Replace(" ", string.Empty)
                             .ToUpper() == rutNormalizado);
            }

            if (usuario == null && int.TryParse(valor.Trim(), out var usuarioId))
            {
                usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            }

            if (usuario == null)
            {
                return Json(new { success = false, message = "No encontramos ningún usuario con ese RUT o ID." });
            }

            return Json(new
            {
                success = true,
                usuario = new
                {
                    id = usuario.Id,
                    nombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                    rut = usuario.RUT,
                    curso = usuario.Curso
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> BuscarEjemplaresDisponibles(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Trim().Length < 2)
            {
                return Json(new { success = true, ejemplares = Array.Empty<object>() });
            }

            var query = termino.Trim();
            var like = $"%{query}%";

            var ejemplares = await _context.Ejemplares
                .Include(e => e.Libro)
                .Where(e => e.Estado == "Disponible" &&
                            (EF.Functions.Like(e.CodigoBarras, like) ||
                             (e.Libro != null && (EF.Functions.Like(e.Libro.Titulo, like) || EF.Functions.Like(e.Libro.Autor, like)))))
                .OrderBy(e => e.Libro!.Titulo)
                .Take(10)
                .Select(e => new
                {
                    codigo = e.CodigoBarras,
                    titulo = e.Libro!.Titulo,
                    autor = e.Libro!.Autor,
                    ubicacion = e.Libro!.Ubicacion
                })
                .ToListAsync();

            return Json(new { success = true, ejemplares });
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPrestamosActivos(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Trim().Length < 2)
            {
                return Json(new { success = true, prestamos = Array.Empty<object>() });
            }

            var query = termino.Trim();
            var like = $"%{query}%";

            var prestamos = await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .Where(p => p.Estado == "Activo" &&
                     p.Ejemplar != null &&
                     p.Ejemplar.Estado == "Prestado" &&
                    (
                     (p.Ejemplar != null && EF.Functions.Like(p.Ejemplar.CodigoBarras, like)) ||
                     (p.Ejemplar != null && p.Ejemplar.Libro != null &&
                        (EF.Functions.Like(p.Ejemplar.Libro.Titulo, like) || EF.Functions.Like(p.Ejemplar.Libro.Autor, like))) ||
                     (p.Usuario != null &&
                        (EF.Functions.Like(p.Usuario.Nombre, like) ||
                         EF.Functions.Like(p.Usuario.Apellido, like) ||
                        EF.Functions.Like(p.Usuario.RUT, like)))
                    ))
                .OrderByDescending(p => p.FechaPrestamo)
                .Take(10)
                .Select(p => new
                {
                    codigo = p.Ejemplar != null ? p.Ejemplar.CodigoBarras : string.Empty,
                    titulo = p.Ejemplar != null && p.Ejemplar.Libro != null ? p.Ejemplar.Libro.Titulo : "Sin título",
                    usuario = p.Usuario != null ? p.Usuario.Nombre + " " + p.Usuario.Apellido : "Usuario",
                    rut = p.Usuario != null ? p.Usuario.RUT : null,
                    fechaVencimiento = p.FechaVencimiento
                })
                .ToListAsync();

            return Json(new { success = true, prestamos });
        }

        private void PrepararEntidadesParaDevolucion(Prestamo prestamo, Ejemplar ejemplar, Usuario usuario)
        {
            prestamo.Estado = "Devuelto";
            prestamo.FechaDevolucion = DateTime.Now;
            _context.Entry(prestamo).State = EntityState.Modified;

            ejemplar.Estado = "Disponible";
            ejemplar.PrestadoA = null;
            ejemplar.FechaPrestamo = null;
            _context.Entry(ejemplar).State = EntityState.Modified;

            if (usuario.PrestamosActivos > 0)
            {
                usuario.PrestamosActivos--;
            }
            _context.Entry(usuario).State = EntityState.Modified;
        }

        private static string NormalizarRut(string? rut)
        {
            if (string.IsNullOrWhiteSpace(rut))
            {
                return string.Empty;
            }

            var cleaned = new string(rut.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

            if (cleaned.All(char.IsDigit) && cleaned.Length >= 12)
            {
                cleaned = cleaned[..^1];
            }

            cleaned = cleaned.TrimStart('0');
            return cleaned;
        }

        private bool TryNormalizarCodigo(string? input, out string codigoNormalizado, out string error)
        {
            codigoNormalizado = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Debes ingresar un código de barras válido.";
                return false;
            }

            if (Ean13Helper.TryNormalize(input.Trim(), out var codigo, out var helperError))
            {
                codigoNormalizado = codigo;
                return true;
            }

            error = helperError;
            return false;
        }

        public class DevolucionPorCodigoRequest
        {
            public string CodigoBarras { get; set; } = string.Empty;
        }

        public class PrestamoRapidoRequest
        {
            public string CodigoBarras { get; set; } = string.Empty;
            public string RutUsuario { get; set; } = string.Empty;
        }

    }
}
