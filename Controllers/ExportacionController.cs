using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BibliotecaVirtualWeb.Services;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class ExportacionController : Controller
    {
        private readonly ExportacionService _exportacion;

        public ExportacionController(ExportacionService exportacion)
        {
            _exportacion = exportacion;
        }

        // GET: Exportacion
        public IActionResult Index()
        {
            return View();
        }

        #region Préstamos Vencidos

        [HttpGet]
        public async Task<IActionResult> PrestamosVencidosExcel()
        {
            try
            {
                var bytes = await _exportacion.ExportarPrestamosVencidosExcel();
                var fileName = $"Prestamos_Vencidos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrestamosVencidosPdf()
        {
            try
            {
                var bytes = await _exportacion.ExportarPrestamosVencidosPdf();
                var fileName = $"Prestamos_Vencidos_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Inventario

        [HttpGet]
        public async Task<IActionResult> InventarioExcel()
        {
            try
            {
                var bytes = await _exportacion.ExportarInventarioExcel();
                var fileName = $"Inventario_Biblioteca_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> InventarioPdf()
        {
            try
            {
                var bytes = await _exportacion.ExportarInventarioPdf();
                var fileName = $"Inventario_Biblioteca_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Historial de Circulación

        [HttpGet]
        public async Task<IActionResult> HistorialExcel(DateTime? desde, DateTime? hasta)
        {
            try
            {
                var bytes = await _exportacion.ExportarHistorialCirculacionExcel(desde, hasta);
                var fileName = $"Historial_Circulacion_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> HistorialPdf(DateTime? desde, DateTime? hasta)
        {
            try
            {
                var bytes = await _exportacion.ExportarHistorialCirculacionPdf(desde, hasta);
                var fileName = $"Historial_Circulacion_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion
    }
}

