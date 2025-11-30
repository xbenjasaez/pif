using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using BibliotecaVirtualWeb.Services;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Linq;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ReportesPdfRenderer _reportesPdfRenderer;

        public ReportesController(ApplicationDbContext context, ReportesPdfRenderer reportesPdfRenderer, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _reportesPdfRenderer = reportesPdfRenderer;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarPdf(
            [Bind(Prefix = "ExportOptions")] ReportesExportOptions exportOptions,
            string periodo = "Historico",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? curso = null,
            string? categoria = null)
        {
            var seleccionValida = exportOptions != null && (exportOptions.IncluirResumen ||
                exportOptions.IncluirPrestamos ||
                exportOptions.IncluirRankings ||
                exportOptions.IncluirEstadisticas ||
                exportOptions.IncluirAlertas);

            if (!seleccionValida)
            {
                TempData["WarningMessage"] = "Debes seleccionar al menos una sección para exportar.";
                return RedirectToAction(nameof(Index), new { periodo, fechaInicio, fechaFin, curso, categoria });
            }

            var viewModel = await ConstruirReporteDetallado(periodo, fechaInicio, fechaFin, curso, categoria);
            viewModel.ExportOptions = exportOptions;

            var pdfBytes = _reportesPdfRenderer.Generar(viewModel, exportOptions);
            var nombreArchivo = $"Reportes_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        private async Task<ReportesDetalladosViewModel> ConstruirReporteDetallado(string periodo, DateTime? fechaInicio, DateTime? fechaFin, string? cursoSeleccionado = null, string? categoriaSeleccionada = null)
        {
            var filtro = new FiltroReportesViewModel { Periodo = periodo };

            DateTime fechaInicioCalculo;
            var fechaFinCalculo = fechaFin ?? DateTime.Now;

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                filtro.Periodo = "Personalizado";
                fechaInicioCalculo = fechaInicio.Value;
                fechaFinCalculo = fechaFin.Value;
            }
            else
            {
                fechaInicioCalculo = periodo switch
                {
                    "Hoy" => DateTime.Today,
                    "Semana" => DateTime.Now.AddDays(-7),
                    "Mes" => DateTime.Now.AddMonths(-1),
                    "SeisMeses" => DateTime.Now.AddMonths(-6),
                    "Anual" => DateTime.Now.AddYears(-1),
                    _ => DateTime.Now.AddYears(-10)
                };
            }

            IQueryable<Prestamo> prestamosFiltrados;
            if (filtro.Periodo == "Historico" && !fechaInicio.HasValue)
            {
                prestamosFiltrados = _context.Prestamos
                    .Include(p => p.Usuario)
                    .Include(p => p.Libro)
                    .Include(p => p.Ejemplar)
                    .AsQueryable();
                var primerPrestamoFecha = await _context.Prestamos
                    .OrderBy(p => p.FechaPrestamo)
                    .Select(p => p.FechaPrestamo)
                    .FirstOrDefaultAsync();
                if (primerPrestamoFecha != default)
                {
                    fechaInicioCalculo = primerPrestamoFecha;
                }
            }
            else
            {
                prestamosFiltrados = _context.Prestamos
                    .Include(p => p.Usuario)
                    .Include(p => p.Libro)
                    .Include(p => p.Ejemplar)
                    .Where(p => p.FechaPrestamo >= fechaInicioCalculo && p.FechaPrestamo <= fechaFinCalculo);
            }

            if (!string.IsNullOrWhiteSpace(cursoSeleccionado) && !string.Equals(cursoSeleccionado, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(cursoSeleccionado, "Sin curso", StringComparison.OrdinalIgnoreCase))
                {
                    prestamosFiltrados = prestamosFiltrados.Where(p => p.Usuario.Curso == null || p.Usuario.Curso == "");
                }
                else
                {
                    prestamosFiltrados = prestamosFiltrados.Where(p => p.Usuario.Curso != null && p.Usuario.Curso == cursoSeleccionado);
                }
            }

            if (!string.IsNullOrWhiteSpace(categoriaSeleccionada) && !string.Equals(categoriaSeleccionada, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(categoriaSeleccionada, "Sin categoría", StringComparison.OrdinalIgnoreCase))
                {
                    prestamosFiltrados = prestamosFiltrados.Where(p => p.Libro.Categoria == null || p.Libro.Categoria == "");
                }
                else
                {
                    prestamosFiltrados = prestamosFiltrados.Where(p => p.Libro.Categoria != null && p.Libro.Categoria == categoriaSeleccionada);
                }
            }

            filtro.FechaInicio = fechaInicioCalculo;
            filtro.FechaFin = fechaFinCalculo;
            filtro.CursoSeleccionado = cursoSeleccionado;
            filtro.CategoriaSeleccionada = categoriaSeleccionada;

            var viewModel = new ReportesDetalladosViewModel
            {
                Filtro = filtro
            };

            viewModel.TotalUsuarios = await _context.Usuarios.CountAsync();
            viewModel.TotalLibros = await _context.Libros.CountAsync();
            viewModel.TotalPrestamos = await prestamosFiltrados.CountAsync();
            viewModel.PrestamosActivos = await prestamosFiltrados.CountAsync(p => p.Estado == "Activo");

            var prestamosDevueltos = await prestamosFiltrados
                .Where(p => p.Estado == "Devuelto" && p.FechaDevolucion.HasValue)
                .AsNoTracking()
                .ToListAsync();

            viewModel.TotalDevueltos = prestamosDevueltos.Count;

            if (viewModel.TotalDevueltos > 0)
            {
                var devolucionesPuntuales = prestamosDevueltos.Count(p => p.FechaDevolucion!.Value <= p.FechaVencimiento);
                var devolucionesTardias = viewModel.TotalDevueltos - devolucionesPuntuales;

                viewModel.TotalDevolucionesPuntuales = devolucionesPuntuales;
                viewModel.TotalDevolucionesTardias = devolucionesTardias;
                viewModel.TasaDevolucionPuntual = Math.Round(devolucionesPuntuales * 100.0 / viewModel.TotalDevueltos, 2);
                viewModel.TasaDevolucionTardia = Math.Round(devolucionesTardias * 100.0 / viewModel.TotalDevueltos, 2);
                viewModel.PromedioTiempoPrestamoEnDias = Math.Round(
                    prestamosDevueltos.Average(p => (p.FechaDevolucion!.Value - p.FechaPrestamo).TotalDays), 1);
            }

            viewModel.LibrosNoDevueltos = viewModel.PrestamosActivos;
            viewModel.PrestamosVencidos = await prestamosFiltrados.CountAsync(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now);

            var prestamosParaUsuarios = await (
                from p in prestamosFiltrados
                join u in _context.Usuarios on p.UsuarioId equals u.Id
                select new
                {
                    p.UsuarioId,
                    u.Nombre,
                    u.Apellido,
                    u.RUT,
                    u.Curso,
                    p.Estado,
                    p.FechaVencimiento
                }).AsNoTracking().ToListAsync();

            var ahora = DateTime.Now;
            viewModel.TopUsuarios = prestamosParaUsuarios
                .GroupBy(p => new { p.UsuarioId, p.Nombre, p.Apellido, p.RUT })
                .Select(g => new UsuarioTopViewModel
                {
                    UsuarioId = g.Key.UsuarioId,
                    NombreCompleto = $"{g.Key.Nombre} {g.Key.Apellido}",
                    RUT = g.Key.RUT,
                    TotalPrestamos = g.Count(),
                    PrestamosActivos = g.Count(p => p.Estado == "Activo"),
                    PrestamosDevueltos = g.Count(p => p.Estado == "Devuelto"),
                    PrestamosVencidos = g.Count(p => p.Estado == "Activo" && p.FechaVencimiento < ahora)
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .Take(10)
                .ToList();

            var prestamosParaLibros = await (
                from p in prestamosFiltrados
                where p.EjemplarId > 0
                join e in _context.Ejemplares on p.EjemplarId equals e.Id
                join l in _context.Libros on e.LibroId equals l.Id
                select new
                {
                    LibroId = l.Id,
                    l.Titulo,
                    l.Autor,
                    l.Categoria,
                    p.Estado
                }).AsNoTracking().ToListAsync();

            var prestamosPorCurso = await (
                from p in prestamosFiltrados
                join u in _context.Usuarios on p.UsuarioId equals u.Id
                join l in _context.Libros on p.LibroId equals l.Id
                select new
                {
                    Curso = string.IsNullOrWhiteSpace(u.Curso) ? "Sin curso" : u.Curso,
                    LibroTitulo = l.Titulo,
                    p.Estado,
                    p.FechaPrestamo,
                    p.FechaVencimiento,
                    p.FechaDevolucion
                }).AsNoTracking().ToListAsync();

            viewModel.TopLibros = prestamosParaLibros
                .GroupBy(p => new { p.LibroId, p.Titulo, p.Autor, p.Categoria })
                .Select(g => new LibroTopViewModel
                {
                    LibroId = g.Key.LibroId,
                    Titulo = g.Key.Titulo,
                    Autor = g.Key.Autor,
                    Categoria = g.Key.Categoria ?? "Sin categoría",
                    TotalPrestamos = g.Count(),
                    PrestamosActivos = g.Count(p => p.Estado == "Activo")
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .Take(10)
                .ToList();

            viewModel.CategoriasPopulares = prestamosParaLibros
                .GroupBy(p => p.Categoria ?? "Sin categoría")
                .Select(g => new CategoriaPopularViewModel
                {
                    Categoria = g.Key,
                    TotalPrestamos = g.Count(),
                    PrestamosActivos = g.Count(p => p.Estado == "Activo")
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .Take(5)
                .ToList();

            var totalCategorias = viewModel.CategoriasPopulares.Sum(c => c.TotalPrestamos);
            if (totalCategorias > 0)
            {
                foreach (var categoria in viewModel.CategoriasPopulares)
                {
                    categoria.Porcentaje = Math.Round(categoria.TotalPrestamos * 100.0 / totalCategorias, 1);
                }
            }

            var mesesAtras = filtro.Periodo switch
            {
                "Hoy" => 1,
                "Semana" => 1,
                "DosMeses" => 2,
                "Anual" => 12,
                _ => 6
            };

            var fechaInicioEstadisticas = DateTime.Now.AddMonths(-mesesAtras);
            var estadisticasTemp = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicioEstadisticas)
                .GroupBy(p => new { p.FechaPrestamo.Year, p.FechaPrestamo.Month, Segmento = string.IsNullOrWhiteSpace(p.Usuario.Curso) ? "Global" : p.Usuario.Curso })
                .Select(g => new
                {
                    Año = g.Key.Year,
                    MesNumero = g.Key.Month,
                    Segmento = g.Key.Segmento,
                    TotalPrestamos = g.Count(),
                    TotalDevoluciones = g.Count(p => p.Estado == "Devuelto")
                }).ToListAsync();

            var culture = new CultureInfo("es-ES");
            var estadisticasFiltradas = estadisticasTemp
                .Where(x =>
                    string.IsNullOrWhiteSpace(filtro.CursoSeleccionado) ||
                    filtro.CursoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase) ||
                    x.Segmento.Equals(filtro.CursoSeleccionado, StringComparison.OrdinalIgnoreCase))
                .Select(x => new EstadisticaMensualViewModel
                {
                    Año = x.Año,
                    Mes = culture.DateTimeFormat.GetMonthName(x.MesNumero),
                    TotalPrestamos = x.TotalPrestamos,
                    TotalDevoluciones = x.TotalDevoluciones,
                    Segmento = string.IsNullOrWhiteSpace(filtro.CursoSeleccionado) || filtro.CursoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                        ? x.Segmento
                        : filtro.CursoSeleccionado
                })
                .OrderBy(x => x.Año)
                .ThenBy(x => x.Mes)
                .ToList();

            viewModel.EstadisticasMensuales = estadisticasFiltradas;

            viewModel.IndiceRotacion = viewModel.TotalLibros > 0
                ? Math.Round((double)viewModel.TotalPrestamos / viewModel.TotalLibros, 2)
                : 0;
            viewModel.UsuarioMasMoroso = viewModel.TopUsuarios.OrderByDescending(u => u.PrestamosVencidos).FirstOrDefault();
            viewModel.LibroMasSolicitado = viewModel.TopLibros.FirstOrDefault();
            viewModel.CategoriaMasPopular = viewModel.CategoriasPopulares.FirstOrDefault();
            viewModel.TasaPrestamosActivos = viewModel.TotalPrestamos > 0
                ? Math.Round(viewModel.PrestamosActivos * 100.0 / viewModel.TotalPrestamos, 1)
                : 0;
            viewModel.EficienciaDevoluciones = viewModel.TotalPrestamos > 0
                ? Math.Round(viewModel.TotalDevueltos * 100.0 / viewModel.TotalPrestamos, 1)
                : 0;

            var cursosDisponibles = await _context.Usuarios
                .Where(u => !string.IsNullOrWhiteSpace(u.Curso))
                .Select(u => u.Curso!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            if (!cursosDisponibles.Any(c => c.Equals("Sin curso", StringComparison.OrdinalIgnoreCase)))
            {
                cursosDisponibles.Add("Sin curso");
            }

            var categoriasDisponibles = await _context.Libros
                .Select(l => l.Categoria ?? "Sin categoría")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            filtro.CursosDisponibles = new[] { "Todos" }.Concat(cursosDisponibles);
            filtro.CategoriasDisponibles = new[] { "Todos" }.Concat(categoriasDisponibles);

            var usuarioActual = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Usuario no identificado";
            viewModel.GeneradoPor = usuarioActual;
            viewModel.ExportOptions ??= new ReportesExportOptions();
            viewModel.AlertasRapidas = new List<string>();

            if (viewModel.PrestamosVencidos > 0)
            {
                viewModel.AlertasRapidas.Add($"Hay {viewModel.PrestamosVencidos} préstamo(s) vencido(s) que requieren seguimiento.");
            }
            if (viewModel.LibrosNoDevueltos > 0)
            {
                viewModel.AlertasRapidas.Add($"{viewModel.LibrosNoDevueltos} libro(s) siguen prestados actualmente.");
            }
            if (viewModel.TasaDevolucionTardia > 20)
            {
                viewModel.AlertasRapidas.Add($"La tasa de devoluciones tardías es {viewModel.TasaDevolucionTardia:0.0}%, superior al 20% recomendado.");
            }
            if (viewModel.TasaPrestamosActivos > 70)
            {
                viewModel.AlertasRapidas.Add($"El {viewModel.TasaPrestamosActivos:0.0}% de los préstamos sigue activo, revisa disponibilidad.");
            }

            viewModel.TotalCursosConPrestamos = prestamosPorCurso
                .Select(x => x.Curso)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .Count();

            var totalPrestamosCursos = prestamosPorCurso.Count;
            var cursosAgrupados = prestamosPorCurso
                .GroupBy(x => x.Curso)
                .Select(g => new CursoActividadViewModel
                {
                    Curso = g.Key,
                    TotalPrestamos = g.Count(),
                    PrestamosActivos = g.Count(x => x.Estado == "Activo"),
                    PrestamosVencidos = g.Count(x => x.Estado == "Activo" && x.FechaVencimiento < ahora),
                    PorcentajeDelTotal = totalPrestamosCursos > 0
                        ? Math.Round(g.Count() * 100.0 / totalPrestamosCursos, 1)
                        : 0,
                    PromedioDiasPrestamo = g.Any(x => x.FechaDevolucion.HasValue)
                        ? Math.Round(g.Where(x => x.FechaDevolucion.HasValue)
                            .Average(x => (x.FechaDevolucion!.Value - x.FechaPrestamo).TotalDays), 1)
                        : 0,
                    LibrosFavoritos = g.GroupBy(x => x.LibroTitulo)
                        .Select(lib => new CursoLibroResumenViewModel
                        {
                            LibroTitulo = lib.Key,
                            TotalPrestamos = lib.Count()
                        })
                        .OrderByDescending(lib => lib.TotalPrestamos)
                        .ThenBy(lib => lib.LibroTitulo)
                        .Take(3)
                        .ToList()
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .ThenBy(x => x.Curso)
                .ToList();

            viewModel.CursosTop = cursosAgrupados.Take(8).ToList();
            viewModel.CursoMasActivo = viewModel.CursosTop.FirstOrDefault();
            viewModel.CursoMayorMora = cursosAgrupados
                .OrderByDescending(x => x.PrestamosVencidos)
                .ThenBy(x => x.Curso)
                .FirstOrDefault();
            viewModel.CursosConRiesgo = cursosAgrupados
                .Where(x => x.PrestamosVencidos > 0)
                .OrderByDescending(x => x.PrestamosVencidos)
                .ThenBy(x => x.Curso)
                .Take(5)
                .ToList();

            var chartData = new ReportesChartViewModel();
            
            // Construir gráfico de tendencia mensual con múltiples cursos
            if (estadisticasTemp.Any())
            {
                // Obtener todos los meses únicos ordenados
                var mesesUnicos = estadisticasTemp
                    .Select(x => new { x.Año, x.MesNumero })
                    .Distinct()
                    .OrderBy(x => x.Año)
                    .ThenBy(x => x.MesNumero)
                    .ToList();

                chartData.TendenciaLabels = mesesUnicos
                    .Select(x => $"{culture.DateTimeFormat.GetMonthName(x.MesNumero)} {x.Año}")
                    .ToList();

                // Si hay filtro de curso específico, mostrar solo ese curso
                if (!string.IsNullOrWhiteSpace(filtro.CursoSeleccionado) && 
                    !filtro.CursoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    var cursoFiltrado = filtro.CursoSeleccionado;
                    var datosCurso = mesesUnicos.Select(mes =>
                    {
                        var estadistica = estadisticasTemp
                            .FirstOrDefault(e => e.Año == mes.Año && 
                                                e.MesNumero == mes.MesNumero && 
                                                e.Segmento == cursoFiltrado);
                        return estadistica?.TotalPrestamos ?? 0;
                    }).ToList();

                    chartData.TendenciaSeries.Add(new ChartSerieViewModel
                    {
                        Label = cursoFiltrado,
                        Valores = datosCurso
                    });
                }
                else
                {
                    // Mostrar los top 8 cursos más activos (sin filtro o "Todos")
                    var cursosTop = estadisticasTemp
                        .GroupBy(x => x.Segmento)
                        .Select(g => new
                        {
                            Curso = g.Key,
                            TotalPrestamos = g.Sum(x => x.TotalPrestamos)
                        })
                        .OrderByDescending(x => x.TotalPrestamos)
                        .Take(8)
                        .Select(x => x.Curso)
                        .ToList();

                    // Si no hay cursos, mostrar "Global"
                    if (!cursosTop.Any())
                    {
                        cursosTop.Add("Global");
                    }

                    // Crear una serie para cada curso top
                    foreach (var curso in cursosTop)
                    {
                        var datosCurso = mesesUnicos.Select(mes =>
                        {
                            var estadistica = estadisticasTemp
                                .FirstOrDefault(e => e.Año == mes.Año && 
                                                    e.MesNumero == mes.MesNumero && 
                                                    e.Segmento == curso);
                            return estadistica?.TotalPrestamos ?? 0;
                        }).ToList();

                        chartData.TendenciaSeries.Add(new ChartSerieViewModel
                        {
                            Label = curso,
                            Valores = datosCurso
                        });
                    }
                }
            }

            if (viewModel.CategoriasPopulares.Any())
            {
                chartData.CategoriasLabels = viewModel.CategoriasPopulares.Select(c => c.Categoria).ToList();
                chartData.CategoriasValores = viewModel.CategoriasPopulares.Select(c => c.TotalPrestamos).ToList();
            }

            chartData.EstadoLabels = new List<string> { "Activos", "Devueltos", "Vencidos" };
            chartData.EstadoValores = new List<int>
            {
                viewModel.PrestamosActivos,
                viewModel.TotalDevueltos,
                viewModel.PrestamosVencidos
            };

            viewModel.ChartData = chartData;

            // Calcular las top 10 editoriales con más libros
            var topEditoriales = await _context.Libros
                .Where(l => !string.IsNullOrWhiteSpace(l.Editorial))
                .GroupBy(l => l.Editorial)
                .Select(g => new
                {
                    Editorial = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(10)
                .ToListAsync();

            if (topEditoriales.Any())
            {
                viewModel.EditorialMasLibros = topEditoriales.First().Editorial;
                viewModel.CantidadLibrosEditorial = topEditoriales.First().Cantidad;
                viewModel.TopEditorialesLabels = topEditoriales.Select(e => e.Editorial ?? "Sin editorial").ToList();
                viewModel.TopEditorialesValores = topEditoriales.Select(e => e.Cantidad).ToList();
            }

            return viewModel;
        }

        // GET: Reportes
        public async Task<IActionResult> Index(string periodo = "Historico", DateTime? fechaInicio = null, DateTime? fechaFin = null, string? curso = null, string? categoria = null)
        {
            try
            {
                var viewModel = await ConstruirReporteDetallado(periodo, fechaInicio, fechaFin, curso, categoria);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Reportes: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error al cargar los reportes: {ex.Message}";
                return View(new ReportesDetalladosViewModel
                {
                    Filtro = new FiltroReportesViewModel
                    {
                        Periodo = periodo,
                        FechaInicio = fechaInicio ?? DateTime.Now.AddMonths(-1),
                        FechaFin = fechaFin ?? DateTime.Now
                    }
                });
            }
        }

        // GET: Reportes/LibrosNoDevueltos
        public async Task<IActionResult> LibrosNoDevueltos(string periodo = "Historico", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                Console.WriteLine($"=== LibrosNoDevueltos - INICIO ===");
                Console.WriteLine($"Periodo: {periodo}");
                Console.WriteLine($"FechaInicio: {fechaInicio}");
                Console.WriteLine($"FechaFin: {fechaFin}");
            DateTime fechaInicioCalculo;
            DateTime fechaFinCalculo = fechaFin ?? DateTime.Now;

            // Calcular fechas según el periodo (misma lógica que Index)
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                periodo = "Personalizado";
                fechaInicioCalculo = fechaInicio.Value;
                fechaFinCalculo = fechaFin.Value;
            }
            else
            {
                switch (periodo)
                {
                    case "Hoy":
                        fechaInicioCalculo = DateTime.Today;
                        break;
                    case "Semana":
                        fechaInicioCalculo = DateTime.Now.AddDays(-7);
                        break;
                    case "Mes":
                        fechaInicioCalculo = DateTime.Now.AddMonths(-1);
                        break;
                    case "SeisMeses":
                        fechaInicioCalculo = DateTime.Now.AddMonths(-6);
                        break;
                    case "Anual":
                        fechaInicioCalculo = DateTime.Now.AddYears(-1);
                        break;
                    case "Historico":
                    default:
                        var primerPrestamoFecha = await _context.Prestamos
                            .OrderBy(p => p.FechaPrestamo)
                            .Select(p => p.FechaPrestamo)
                            .FirstOrDefaultAsync();
                        fechaInicioCalculo = primerPrestamoFecha != default ? primerPrestamoFecha : DateTime.Now.AddYears(-10);
                        break;
                }
            }

            // Obtener préstamos activos (no devueltos)
            var prestamosActivosTemp = await (
                from p in _context.Prestamos
                where p.Estado == "Activo" 
                    && p.FechaPrestamo >= fechaInicioCalculo 
                    && p.FechaPrestamo <= fechaFinCalculo
                join u in _context.Usuarios on p.UsuarioId equals u.Id
                join e in _context.Ejemplares on p.EjemplarId equals e.Id
                join l in _context.Libros on e.LibroId equals l.Id
                orderby p.FechaVencimiento
                select new
                {
                    p.Id,
                    LibroTitulo = l.Titulo,
                    LibroAutor = l.Autor,
                    EjemplarCodigo = e.CodigoBarras,
                    UsuarioNombre = u.Nombre + " " + u.Apellido,
                    UsuarioRUT = u.RUT,
                    p.FechaPrestamo,
                    p.FechaVencimiento
                }
            ).ToListAsync();

            // Calcular días y estado en memoria
            var ahora = DateTime.Now;
            var prestamosActivos = prestamosActivosTemp.Select(p => new PrestamoActivoViewModel
            {
                Id = p.Id,
                LibroTitulo = p.LibroTitulo,
                LibroAutor = p.LibroAutor,
                EjemplarCodigo = p.EjemplarCodigo,
                UsuarioNombre = p.UsuarioNombre,
                UsuarioRUT = p.UsuarioRUT,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                DiasTranscurridos = (int)(ahora - p.FechaPrestamo).TotalDays,
                EstaVencido = p.FechaVencimiento < ahora
            }).ToList();

            ViewBag.Periodo = periodo;
            ViewBag.FechaInicio = fechaInicioCalculo;
            ViewBag.FechaFin = fechaFinCalculo;
            ViewBag.TotalActivos = prestamosActivos.Count;
            ViewBag.TotalVencidos = prestamosActivos.Count(p => p.EstaVencido);

            Console.WriteLine($"Total préstamos activos encontrados: {prestamosActivos.Count}");
            Console.WriteLine($"=== LibrosNoDevueltos - FIN ===");

            return View(prestamosActivos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en LibrosNoDevueltos:");
                Console.WriteLine($"   Mensaje: {ex.Message}");
                Console.WriteLine($"   Tipo: {ex.GetType().Name}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }
                
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ErrorDetail = ex.ToString();
                return View("Error");
            }
        }

        // GET: Reportes/PrestamosVencidos
        public async Task<IActionResult> PrestamosVencidos(string periodo = "Historico", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                Console.WriteLine($"=== PrestamosVencidos - INICIO ===");
                Console.WriteLine($"Periodo: {periodo}");
                Console.WriteLine($"FechaInicio: {fechaInicio}");
                Console.WriteLine($"FechaFin: {fechaFin}");
            DateTime fechaInicioCalculo;
            DateTime fechaFinCalculo = fechaFin ?? DateTime.Now;

            // Calcular fechas según el periodo
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                periodo = "Personalizado";
                fechaInicioCalculo = fechaInicio.Value;
                fechaFinCalculo = fechaFin.Value;
            }
            else
            {
                switch (periodo)
                {
                    case "Hoy":
                        fechaInicioCalculo = DateTime.Today;
                        break;
                    case "Semana":
                        fechaInicioCalculo = DateTime.Now.AddDays(-7);
                        break;
                    case "Mes":
                        fechaInicioCalculo = DateTime.Now.AddMonths(-1);
                        break;
                    case "SeisMeses":
                        fechaInicioCalculo = DateTime.Now.AddMonths(-6);
                        break;
                    case "Anual":
                        fechaInicioCalculo = DateTime.Now.AddYears(-1);
                        break;
                    case "Historico":
                    default:
                        var primerPrestamoFecha = await _context.Prestamos
                            .OrderBy(p => p.FechaPrestamo)
                            .Select(p => p.FechaPrestamo)
                            .FirstOrDefaultAsync();
                        fechaInicioCalculo = primerPrestamoFecha != default ? primerPrestamoFecha : DateTime.Now.AddYears(-10);
                        break;
                }
            }

            var ahora = DateTime.Now;
            
            // Obtener préstamos vencidos
            var prestamosVencidosTemp = await (
                from p in _context.Prestamos
                where p.Estado == "Activo" 
                    && p.FechaVencimiento < ahora
                    && p.FechaPrestamo >= fechaInicioCalculo 
                    && p.FechaPrestamo <= fechaFinCalculo
                join u in _context.Usuarios on p.UsuarioId equals u.Id
                join e in _context.Ejemplares on p.EjemplarId equals e.Id
                join l in _context.Libros on e.LibroId equals l.Id
                orderby p.FechaVencimiento
                select new
                {
                    p.Id,
                    LibroTitulo = l.Titulo,
                    LibroAutor = l.Autor,
                    EjemplarCodigo = e.CodigoBarras,
                    UsuarioNombre = u.Nombre + " " + u.Apellido,
                    UsuarioRUT = u.RUT,
                    UsuarioTelefono = u.Telefono,
                    UsuarioEmail = u.Email,
                    p.FechaPrestamo,
                    p.FechaVencimiento
                }
            ).ToListAsync();

            // Calcular días vencidos en memoria
            var prestamosVencidos = prestamosVencidosTemp.Select(p => new PrestamoVencidoViewModel
            {
                Id = p.Id,
                LibroTitulo = p.LibroTitulo,
                LibroAutor = p.LibroAutor,
                EjemplarCodigo = p.EjemplarCodigo,
                UsuarioNombre = p.UsuarioNombre,
                UsuarioRUT = p.UsuarioRUT,
                UsuarioTelefono = p.UsuarioTelefono,
                UsuarioEmail = p.UsuarioEmail,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                DiasVencido = (int)(ahora - p.FechaVencimiento).TotalDays
            }).ToList();

            ViewBag.Periodo = periodo;
            ViewBag.FechaInicio = fechaInicioCalculo;
            ViewBag.FechaFin = fechaFinCalculo;
            ViewBag.TotalVencidos = prestamosVencidos.Count;
            ViewBag.Vencidos15Dias = prestamosVencidos.Count(p => p.DiasVencido > 15);
            ViewBag.Vencidos30Dias = prestamosVencidos.Count(p => p.DiasVencido > 30);

            Console.WriteLine($"Total préstamos vencidos encontrados: {prestamosVencidos.Count}");
            Console.WriteLine($"=== PrestamosVencidos - FIN ===");

            return View(prestamosVencidos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en PrestamosVencidos:");
                Console.WriteLine($"   Mensaje: {ex.Message}");
                Console.WriteLine($"   Tipo: {ex.GetType().Name}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }
                
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ErrorDetail = ex.ToString();
                return View("Error");
            }
        }
    }
}

