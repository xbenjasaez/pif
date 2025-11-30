using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BibliotecaVirtualWeb.Services;
using System.Collections.Concurrent;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        private readonly BackupService _backupService;
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<string, BackupProgress> _progress = new();

        public BackupController(BackupService backupService, IServiceProvider serviceProvider)
        {
            _backupService = backupService;
            _serviceProvider = serviceProvider;
        }

        // GET: Backup
        public async Task<IActionResult> Index()
        {
            var backups = await _backupService.ObtenerBackupsAsync();
            return View(backups);
        }

        // POST: Backup/Crear (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(string? descripcion)
        {
            var taskId = Guid.NewGuid().ToString();
            var progress = new BackupProgress
            {
                TaskId = taskId,
                Estado = "Iniciando...",
                Porcentaje = 0,
                Inicio = DateTime.Now
            };
            _progress[taskId] = progress;

            // Ejecutar backup en background con scope de servicio propio
            _ = Task.Run(async () =>
            {
                // Crear un scope de servicio para obtener una nueva instancia del DbContext
                using (var scope = _serviceProvider.CreateScope())
                {
                    var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
                    
                    try
                    {
                        progress.Estado = "Conectando a la base de datos...";
                        progress.Porcentaje = 10;

                        var resultado = await backupService.GenerarBackupAsync(descripcion, progress);

                        // Solo actualizar si el servicio no lo hizo ya
                        if (progress.Porcentaje < 100)
                        {
                            progress.Porcentaje = 100;
                        }
                        
                        progress.Duracion = DateTime.Now - progress.Inicio;

                        if (resultado.Exitoso)
                        {
                            // Solo actualizar si no está ya en "Completado"
                            if (!progress.Estado.Contains("Completado"))
                            {
                                progress.Estado = "Completado";
                            }
                            progress.Exitoso = true;
                            progress.Mensaje = $"Backup creado: {resultado.NombreArchivo}";
                            progress.Resultado = resultado;
                        }
                        else
                        {
                            progress.Estado = "Error";
                            progress.Exitoso = false;
                            progress.Mensaje = resultado.Mensaje ?? "Error desconocido";
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.Estado = "Error";
                        progress.Exitoso = false;
                        progress.Mensaje = $"Error: {ex.Message}";
                    }
                }
            });

            return Json(new { taskId });
        }

        // GET: Backup/Progreso/{taskId}
        [HttpGet]
        public IActionResult Progreso(string taskId)
        {
            if (_progress.TryGetValue(taskId, out var progress))
            {
                return Json(new
                {
                    taskId = progress.TaskId,
                    estado = progress.Estado,
                    porcentaje = progress.Porcentaje,
                    inicio = progress.Inicio,
                    duracion = progress.Duracion,
                    exitoso = progress.Exitoso,
                    mensaje = progress.Mensaje,
                    resultado = progress.Resultado != null ? new
                    {
                        exitoso = progress.Resultado.Exitoso,
                        nombreArchivo = progress.Resultado.NombreArchivo,
                        tamañoBytes = progress.Resultado.TamañoBytes,
                        mensaje = progress.Resultado.Mensaje
                    } : null
                });
            }
            return Json(new { error = "Tarea no encontrada" });
        }

        // GET: Backup/Descargar/5
        public async Task<IActionResult> Descargar(int id)
        {
            var ruta = await _backupService.ObtenerRutaBackupAsync(id);
            
            if (ruta == null)
            {
                TempData["ErrorMessage"] = "Backup no encontrado";
                return RedirectToAction(nameof(Index));
            }

            var nombreArchivo = Path.GetFileName(ruta);
            var contenido = await System.IO.File.ReadAllBytesAsync(ruta);
            
            return File(contenido, "application/sql", nombreArchivo);
        }

        // POST: Backup/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _backupService.EliminarBackupAsync(id);

            if (eliminado)
            {
                TempData["SuccessMessage"] = "Backup eliminado correctamente";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el backup";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Backup/Limpiar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Limpiar(int mantener = 10)
        {
            var eliminados = await _backupService.LimpiarBackupsAntiguosAsync(mantener);

            if (eliminados > 0)
            {
                TempData["SuccessMessage"] = $"Se eliminaron {eliminados} backup(s) antiguo(s)";
            }
            else
            {
                TempData["InfoMessage"] = "No hay backups antiguos para eliminar";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class BackupProgress : IBackupProgress
    {
        public string TaskId { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Porcentaje { get; set; }
        public DateTime Inicio { get; set; }
        public TimeSpan? Duracion { get; set; }
        public bool? Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public BackupResult? Resultado { get; set; }
    }
}

