using BibliotecaVirtualWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportacionController : Controller
    {
        private readonly ImportadorService _importadorService;
        private readonly ILogger<ImportacionController> _logger;
        private readonly IWebHostEnvironment _environment;
        
        // Cache estático temporal para resultados de validación (evita usar cookies grandes)
        private static ResultadoImportacion? _resultadoAlumnosCache;
        private static ResultadoImportacion? _resultadoLibrosCache;
        
        // Rutas de archivos temporales
        private static string? _archivoAlumnosTempPath;
        private static string? _archivoLibrosTempPath;
        private static string? _nombreArchivoAlumnos;
        private static string? _nombreArchivoLibros;

        public ImportacionController(
            ImportadorService importadorService, 
            ILogger<ImportacionController> logger,
            IWebHostEnvironment environment)
        {
            _importadorService = importadorService;
            _logger = logger;
            _environment = environment;
        }
        
        private string GetTempDirectory()
        {
            var tempDir = Path.Combine(_environment.ContentRootPath, "temp_imports");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            return tempDir;
        }

        // GET: Importacion
        public IActionResult Index()
        {
            return View();
        }

        // POST: Importacion/ValidarAlumnos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarAlumnos(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un archivo CSV.";
                return RedirectToAction(nameof(Index));
            }

            if (!archivo.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "El archivo debe ser CSV.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Guardar archivo temporalmente para usarlo en la confirmación
                var tempPath = Path.Combine(GetTempDirectory(), $"alumnos_{Guid.NewGuid()}.csv");
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await archivo.CopyToAsync(fileStream);
                }
                
                // Limpiar archivo temporal anterior si existe
                if (!string.IsNullOrEmpty(_archivoAlumnosTempPath) && System.IO.File.Exists(_archivoAlumnosTempPath))
                {
                    System.IO.File.Delete(_archivoAlumnosTempPath);
                }
                
                _archivoAlumnosTempPath = tempPath;
                _nombreArchivoAlumnos = archivo.FileName;
                
                // Validar el archivo
                using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                var resultado = await _importadorService.ImportarAlumnosAsync(stream, soloValidar: true);

                // Guardar en cache de memoria (no en cookies)
                _resultadoAlumnosCache = resultado;
                return RedirectToAction(nameof(ResultadoAlumnos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar archivo de alumnos");
                TempData["ErrorMessage"] = $"Error al procesar el archivo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Importacion/ResultadoAlumnos
        public IActionResult ResultadoAlumnos()
        {
            if (_resultadoAlumnosCache == null)
            {
                TempData["ErrorMessage"] = "No hay resultados de validación. Por favor, suba el archivo nuevamente.";
                return RedirectToAction(nameof(Index));
            }

            // Crear resumen ligero para la vista (sin las entidades completas)
            var resumen = CrearResumenLigero(_resultadoAlumnosCache);
            ViewBag.NombreArchivo = _nombreArchivoAlumnos;
            return View(resumen);
        }

        // POST: Importacion/ConfirmarAlumnos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarAlumnos()
        {
            // Usar el archivo guardado temporalmente
            if (string.IsNullOrEmpty(_archivoAlumnosTempPath) || !System.IO.File.Exists(_archivoAlumnosTempPath))
            {
                TempData["ErrorMessage"] = "El archivo temporal expiró. Por favor, suba el archivo nuevamente.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = new FileStream(_archivoAlumnosTempPath, FileMode.Open, FileAccess.Read);
                var resultado = await _importadorService.ImportarAlumnosAsync(stream, soloValidar: false);

                // Limpiar cache y archivo temporal
                _resultadoAlumnosCache = null;
                if (System.IO.File.Exists(_archivoAlumnosTempPath))
                {
                    System.IO.File.Delete(_archivoAlumnosTempPath);
                }
                _archivoAlumnosTempPath = null;
                _nombreArchivoAlumnos = null;

                if (resultado.Aplicado)
                {
                    TempData["SuccessMessage"] = $"Importación completada: {resultado.TotalNuevos} nuevos, {resultado.TotalActualizaciones} actualizados.";
                }
                else if (resultado.TieneErrores)
                {
                    TempData["ErrorMessage"] = $"Error en la importación: {string.Join("; ", resultado.Errores.Take(3))}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar alumnos");
                TempData["ErrorMessage"] = $"Error al importar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Importacion/ValidarLibros
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarLibros(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un archivo CSV.";
                return RedirectToAction(nameof(Index));
            }

            if (!archivo.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "El archivo debe ser CSV.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Guardar archivo temporalmente para usarlo en la confirmación
                var tempPath = Path.Combine(GetTempDirectory(), $"libros_{Guid.NewGuid()}.csv");
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await archivo.CopyToAsync(fileStream);
                }
                
                // Limpiar archivo temporal anterior si existe
                if (!string.IsNullOrEmpty(_archivoLibrosTempPath) && System.IO.File.Exists(_archivoLibrosTempPath))
                {
                    System.IO.File.Delete(_archivoLibrosTempPath);
                }
                
                _archivoLibrosTempPath = tempPath;
                _nombreArchivoLibros = archivo.FileName;
                
                // Validar el archivo
                using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                var resultado = await _importadorService.ImportarLibrosAsync(stream, soloValidar: true);

                // Guardar en cache de memoria (no en cookies)
                _resultadoLibrosCache = resultado;
                return RedirectToAction(nameof(ResultadoLibros));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar archivo de libros");
                TempData["ErrorMessage"] = $"Error al procesar el archivo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Importacion/ResultadoLibros
        public IActionResult ResultadoLibros()
        {
            if (_resultadoLibrosCache == null)
            {
                TempData["ErrorMessage"] = "No hay resultados de validación. Por favor, suba el archivo nuevamente.";
                return RedirectToAction(nameof(Index));
            }

            // Crear resumen ligero para la vista (sin las entidades completas)
            var resumen = CrearResumenLigero(_resultadoLibrosCache);
            ViewBag.NombreArchivo = _nombreArchivoLibros;
            return View(resumen);
        }

        // POST: Importacion/ConfirmarLibros
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarLibros()
        {
            // Usar el archivo guardado temporalmente
            if (string.IsNullOrEmpty(_archivoLibrosTempPath) || !System.IO.File.Exists(_archivoLibrosTempPath))
            {
                TempData["ErrorMessage"] = "El archivo temporal expiró. Por favor, suba el archivo nuevamente.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = new FileStream(_archivoLibrosTempPath, FileMode.Open, FileAccess.Read);
                var resultado = await _importadorService.ImportarLibrosAsync(stream, soloValidar: false);

                // Limpiar cache y archivo temporal
                _resultadoLibrosCache = null;
                if (System.IO.File.Exists(_archivoLibrosTempPath))
                {
                    System.IO.File.Delete(_archivoLibrosTempPath);
                }
                _archivoLibrosTempPath = null;
                _nombreArchivoLibros = null;

                if (resultado.Aplicado)
                {
                    TempData["SuccessMessage"] = $"Importación completada: {resultado.TotalNuevos} libros nuevos, {resultado.TotalActualizaciones} con ejemplares adicionales.";
                }
                else if (resultado.TieneErrores)
                {
                    TempData["ErrorMessage"] = $"Error en la importación: {string.Join("; ", resultado.Errores.Take(3))}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar libros");
                TempData["ErrorMessage"] = $"Error al importar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Crear un resumen ligero sin las entidades (solo descripciones)
        private ResultadoImportacion CrearResumenLigero(ResultadoImportacion original)
        {
            return new ResultadoImportacion
            {
                TipoImportacion = original.TipoImportacion,
                Errores = original.Errores,
                Aplicado = original.Aplicado,
                NuevosRegistros = original.NuevosRegistros.Select(r => new RegistroImportacion
                {
                    Descripcion = r.Descripcion,
                    Entidad = null // No pasar la entidad completa
                }).ToList(),
                ActualizacionesPendientes = original.ActualizacionesPendientes.Select(r => new RegistroImportacion
                {
                    Descripcion = r.Descripcion,
                    Entidad = null
                }).ToList()
            };
        }
    }
}

