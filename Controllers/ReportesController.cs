using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace BibliotecaVirtualWeb.Controllers
{
    [Authorize(Roles = "Admin,Bibliotecario")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reportes
        public async Task<IActionResult> Index(string periodo = "Historico", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                var viewModel = new ReportesDetalladosViewModel
                {
                    Filtro = new FiltroReportesViewModel { Periodo = periodo }
                };

            // Calcular fechas según el periodo
            DateTime fechaInicioCalculo;
            DateTime fechaFinCalculo = fechaFin ?? DateTime.Now;

            // Si se proporcionan fechas personalizadas, usarlas
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                periodo = "Personalizado";
                fechaInicioCalculo = fechaInicio.Value;
                fechaFinCalculo = fechaFin.Value;
            }
            else
            {
                // Usar periodos predefinidos
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
                        // Para histórico, no aplicar filtro de fecha
                        fechaInicioCalculo = DateTime.Now.AddYears(-10);
                        break;
                }
            }

            // Filtrar préstamos según el periodo
            IQueryable<Prestamo> prestamosFiltrados;
            
            if (periodo == "Historico" && !fechaInicio.HasValue)
            {
                // Para "Histórico", no aplicar ningún filtro de fecha
                prestamosFiltrados = _context.Prestamos.AsQueryable();
                // Para visualización, obtener la fecha del préstamo más antiguo
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
                prestamosFiltrados = _context.Prestamos.Where(p => p.FechaPrestamo >= fechaInicioCalculo && p.FechaPrestamo <= fechaFinCalculo);
            }

            viewModel.Filtro.Periodo = periodo;
            viewModel.Filtro.FechaInicio = fechaInicioCalculo;
            viewModel.Filtro.FechaFin = fechaFinCalculo;
            
            Console.WriteLine($"Filtro aplicado - Periodo: {periodo}, Fecha inicio: {fechaInicioCalculo:yyyy-MM-dd}, Fecha fin: {fechaFinCalculo:yyyy-MM-dd}");

            // Estadísticas generales
            viewModel.TotalUsuarios = await _context.Usuarios.CountAsync();
            viewModel.TotalLibros = await _context.Libros.CountAsync();
            
            // Debug: contar préstamos totales en la BD
            var totalPrestamosEnBD = await _context.Prestamos.CountAsync();
            Console.WriteLine($"Total préstamos en BD: {totalPrestamosEnBD}");
            Console.WriteLine($"Periodo seleccionado: {periodo}");
            Console.WriteLine($"Fecha inicio filtro: {fechaInicio}");
            
            viewModel.TotalPrestamos = await prestamosFiltrados.CountAsync();
            Console.WriteLine($"Total préstamos filtrados: {viewModel.TotalPrestamos}");
            
            viewModel.PrestamosActivos = await prestamosFiltrados.CountAsync(p => p.Estado == "Activo");
            Console.WriteLine($"Préstamos activos: {viewModel.PrestamosActivos}");

            // Calcular tasa de devolución
            var prestamosDevueltos = await prestamosFiltrados
                .Where(p => p.Estado == "Devuelto" && p.FechaDevolucion.HasValue)
                .ToListAsync();

            viewModel.TotalDevueltos = prestamosDevueltos.Count;
            
            if (viewModel.TotalDevueltos > 0)
            {
                var devolucionesPuntuales = prestamosDevueltos
                    .Count(p => p.FechaDevolucion!.Value <= p.FechaVencimiento);
                var devolucionesTardias = viewModel.TotalDevueltos - devolucionesPuntuales;

                viewModel.TotalDevolucionesPuntuales = devolucionesPuntuales;
                viewModel.TotalDevolucionesTardias = devolucionesTardias;
                viewModel.TasaDevolucionPuntual = Math.Round((devolucionesPuntuales * 100.0) / viewModel.TotalDevueltos, 2);
                viewModel.TasaDevolucionTardia = Math.Round((devolucionesTardias * 100.0) / viewModel.TotalDevueltos, 2);
            }

            // Libros no devueltos y préstamos vencidos
            viewModel.LibrosNoDevueltos = await prestamosFiltrados
                .CountAsync(p => p.Estado == "Activo");

            viewModel.PrestamosVencidos = await prestamosFiltrados
                .CountAsync(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now);

            // Tiempo promedio de préstamo
            if (viewModel.TotalDevueltos > 0)
            {
                var tiemposPromedio = prestamosDevueltos
                    .Select(p => (p.FechaDevolucion!.Value - p.FechaPrestamo).TotalDays)
                    .ToList();

                viewModel.PromedioTiempoPrestamoEnDias = Math.Round(tiemposPromedio.Average(), 1);
            }

            // Top 10 Usuarios - Hacer join explícito
            var prestamosParaUsuarios = await (
                from p in prestamosFiltrados
                join u in _context.Usuarios on p.UsuarioId equals u.Id
                select new {
                    p.UsuarioId,
                    u.Nombre,
                    u.Apellido,
                    u.RUT,
                    p.Estado,
                    p.FechaVencimiento
                }
            ).ToListAsync();
            
            Console.WriteLine($"Préstamos para usuarios: {prestamosParaUsuarios.Count}");

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

            // Top 10 Libros más pedidos - Hacer joins explícitos
            var prestamosParaLibros = await (
                from p in prestamosFiltrados
                where p.EjemplarId > 0
                join e in _context.Ejemplares on p.EjemplarId equals e.Id
                join l in _context.Libros on e.LibroId equals l.Id
                select new {
                    LibroId = l.Id,
                    l.Titulo,
                    l.Autor,
                    l.Categoria,
                    p.Estado
                }
            ).ToListAsync();
            
            Console.WriteLine($"Préstamos para libros: {prestamosParaLibros.Count}");

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

            // Categorías más populares - Usar los mismos datos que libros
            var prestamosParaCategorias = prestamosParaLibros
                .Select(p => new {
                    Categoria = p.Categoria ?? "Sin categoría",
                    p.Estado
                })
                .ToList();

            viewModel.CategoriasPopulares = prestamosParaCategorias
                .GroupBy(p => p.Categoria)
                .Select(g => new CategoriaPopularViewModel
                {
                    Categoria = g.Key,
                    TotalPrestamos = g.Count(),
                    PrestamosActivos = g.Count(p => p.Estado == "Activo"),
                    Porcentaje = 0
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .Take(5)
                .ToList();

            // Calcular porcentajes de categorías
            var totalCategorias = viewModel.CategoriasPopulares.Sum(c => c.TotalPrestamos);
            if (totalCategorias > 0)
            {
                foreach (var categoria in viewModel.CategoriasPopulares)
                {
                    categoria.Porcentaje = Math.Round((categoria.TotalPrestamos * 100.0) / totalCategorias, 1);
                }
            }

            // Estadísticas mensuales (últimos 6 meses o según filtro)
            var mesesAtras = periodo switch
            {
                "Hoy" => 1,
                "Semana" => 1,
                "DosMeses" => 2,
                "Año" => 12,
                _ => 6
            };

            var fechaInicioEstadisticas = DateTime.Now.AddMonths(-mesesAtras);
            
            // Primero traer los datos agrupados
            var estadisticasTemp = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicioEstadisticas)
                .GroupBy(p => new { p.FechaPrestamo.Year, p.FechaPrestamo.Month })
                .Select(g => new
                {
                    Año = g.Key.Year,
                    MesNumero = g.Key.Month,
                    TotalPrestamos = g.Count(),
                    TotalDevoluciones = g.Count(p => p.Estado == "Devuelto")
                })
                .ToListAsync();

            Console.WriteLine($"Estadísticas mensuales obtenidas: {estadisticasTemp.Count}");

            // Formatear nombres de meses y crear el ViewModel
            var culture = new CultureInfo("es-ES");
            viewModel.EstadisticasMensuales = estadisticasTemp
                .OrderBy(x => x.Año)
                .ThenBy(x => x.MesNumero)
                .Select(x => new EstadisticaMensualViewModel
                {
                    Año = x.Año,
                    Mes = culture.DateTimeFormat.GetMonthName(x.MesNumero),
                    TotalPrestamos = x.TotalPrestamos,
                    TotalDevoluciones = x.TotalDevoluciones
                })
                .ToList();

            // Estadísticas adicionales para la pestaña avanzada
            // Calcular índice de rotación (préstamos / libros)
            ViewBag.IndiceRotacion = viewModel.TotalLibros > 0 
                ? Math.Round((double)viewModel.TotalPrestamos / viewModel.TotalLibros, 2) 
                : 0;

            // Usuario con más préstamos vencidos
            ViewBag.UsuarioMasMoroso = viewModel.TopUsuarios
                .OrderByDescending(u => u.PrestamosVencidos)
                .FirstOrDefault();

            // Libro más solicitado
            ViewBag.LibroMasSolicitado = viewModel.TopLibros.FirstOrDefault();

            // Categoría más popular
            ViewBag.CategoriaMasPopular = viewModel.CategoriasPopulares.FirstOrDefault();

            // Calcular tasa de préstamos activos
            ViewBag.TasaPrestamosActivos = viewModel.TotalPrestamos > 0
                ? Math.Round((viewModel.PrestamosActivos * 100.0) / viewModel.TotalPrestamos, 1)
                : 0;

            // Eficiencia de devoluciones (% de devueltos del total)
            ViewBag.EficienciaDevoluciones = viewModel.TotalPrestamos > 0
                ? Math.Round((viewModel.TotalDevueltos * 100.0) / viewModel.TotalPrestamos, 1)
                : 0;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log del error completo
                Console.WriteLine($"Error en Reportes: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                // Pasar el error a la vista
                ViewBag.ErrorMessage = $"Error al cargar los reportes: {ex.Message}";
                
                // Retornar un modelo vacío para que la vista no falle
                return View(new ReportesDetalladosViewModel
                {
                    Filtro = new FiltroReportesViewModel { Periodo = periodo }
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

