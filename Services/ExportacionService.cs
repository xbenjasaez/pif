using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Services
{
    public class ExportacionService
    {
        private readonly ApplicationDbContext _context;

        public ExportacionService(ApplicationDbContext context)
        {
            _context = context;
            // Configurar licencia de QuestPDF (Community es gratuita)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Préstamos Vencidos

        public async Task<byte[]> ExportarPrestamosVencidosExcel()
        {
            var prestamos = await ObtenerPrestamosVencidos();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Préstamos Vencidos");

            // Encabezados
            var headers = new[] { "Código", "Libro", "Autor", "Usuario", "RUT", "Curso", "F. Préstamo", "F. Vencimiento", "Días Vencido" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Datos
            int row = 2;
            foreach (var p in prestamos)
            {
                worksheet.Cell(row, 1).Value = p.Ejemplar?.CodigoBarras ?? "";
                worksheet.Cell(row, 2).Value = p.Ejemplar?.Libro?.Titulo ?? p.Libro?.Titulo ?? "";
                worksheet.Cell(row, 3).Value = p.Ejemplar?.Libro?.Autor ?? p.Libro?.Autor ?? "";
                worksheet.Cell(row, 4).Value = p.Usuario?.NombreCompleto ?? "";
                worksheet.Cell(row, 5).Value = p.Usuario?.RUT ?? "";
                worksheet.Cell(row, 6).Value = p.Usuario?.CursoConLetra ?? "";
                worksheet.Cell(row, 7).Value = p.FechaPrestamo.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 8).Value = p.FechaVencimiento.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 9).Value = (DateTime.Now - p.FechaVencimiento).Days;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportarPrestamosVencidosPdf()
        {
            var prestamos = await ObtenerPrestamosVencidos();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => CrearEncabezadoPdf(c, "Reporte de Préstamos Vencidos", $"Total: {prestamos.Count} préstamos"));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Código
                            columns.RelativeColumn(4); // Libro
                            columns.RelativeColumn(2); // Usuario
                            columns.RelativeColumn(2); // RUT
                            columns.RelativeColumn(1); // Curso
                            columns.RelativeColumn(2); // F. Vencimiento
                            columns.RelativeColumn(1); // Días
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Código").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Libro").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Usuario").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("RUT").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Curso").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Vencimiento").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Días").Bold();
                        });

                        // Datos
                        foreach (var p in prestamos)
                        {
                            var diasVencido = (DateTime.Now - p.FechaVencimiento).Days;
                            var bgColor = diasVencido > 7 ? Colors.Red.Lighten4 : Colors.Orange.Lighten4;

                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Ejemplar?.CodigoBarras ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Ejemplar?.Libro?.Titulo ?? p.Libro?.Titulo ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Usuario?.NombreCompleto ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Usuario?.RUT ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Usuario?.CursoConLetra ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.FechaVencimiento.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(diasVencido.ToString()).Bold();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<List<Prestamo>> ObtenerPrestamosVencidos()
        {
            return await _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Libro)
                .Include(p => p.Usuario)
                .Where(p => p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now)
                .OrderBy(p => p.FechaVencimiento)
                .ToListAsync();
        }

        #endregion

        #region Inventario Completo

        public async Task<byte[]> ExportarInventarioExcel()
        {
            var ejemplares = await ObtenerInventarioCompleto();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventario");

            // Encabezados
            var headers = new[] { "Código Barras", "Título", "Autor", "Editorial", "ISBN", "Categoría", "Ubicación", "Estado", "Notas", "Prestado A" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Datos
            int row = 2;
            foreach (var e in ejemplares)
            {
                worksheet.Cell(row, 1).Value = e.CodigoBarras;
                worksheet.Cell(row, 2).Value = e.Libro?.Titulo ?? "";
                worksheet.Cell(row, 3).Value = e.Libro?.Autor ?? "";
                worksheet.Cell(row, 4).Value = e.Libro?.Editorial ?? "";
                worksheet.Cell(row, 5).Value = e.Libro?.ISBN ?? "";
                worksheet.Cell(row, 6).Value = e.Libro?.Categoria ?? "";
                worksheet.Cell(row, 7).Value = e.Ubicacion ?? "";
                worksheet.Cell(row, 8).Value = e.Estado;
                worksheet.Cell(row, 9).Value = e.Notas ?? "";
                worksheet.Cell(row, 10).Value = e.PrestadoA ?? "";
                
                // Color según estado
                if (e.Estado == "Deteriorado")
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                else if (e.Estado == "Prestado")
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportarInventarioPdf()
        {
            var ejemplares = await ObtenerInventarioCompleto();
            var resumen = new
            {
                Total = ejemplares.Count,
                Disponibles = ejemplares.Count(e => e.Estado == "Disponible"),
                Prestados = ejemplares.Count(e => e.Estado == "Prestado"),
                Deteriorados = ejemplares.Count(e => e.Estado == "Deteriorado"),
                Otros = ejemplares.Count(e => !new[] { "Disponible", "Prestado", "Deteriorado" }.Contains(e.Estado))
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => CrearEncabezadoPdf(c, "Inventario Completo de Biblioteca", 
                        $"Total: {resumen.Total} | Disponibles: {resumen.Disponibles} | Prestados: {resumen.Prestados} | Deteriorados: {resumen.Deteriorados}"));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Código
                            columns.RelativeColumn(4); // Título
                            columns.RelativeColumn(2); // Autor
                            columns.RelativeColumn(2); // Ubicación
                            columns.RelativeColumn(1.5f); // Estado
                            columns.RelativeColumn(2); // Prestado A
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Código").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Título").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Autor").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ubicación").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Estado").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Prestado A").Bold();
                        });

                        // Datos
                        foreach (var e in ejemplares)
                        {
                            var bgColor = e.Estado switch
                            {
                                "Prestado" => Colors.Red.Lighten4,
                                "Deteriorado" => Colors.Orange.Lighten4,
                                "Disponible" => Colors.Green.Lighten4,
                                _ => Colors.Grey.Lighten4
                            };

                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.CodigoBarras);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.Libro?.Titulo ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.Libro?.Autor ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.Ubicacion ?? "-");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.Estado);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(e.PrestadoA ?? "-");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<List<Ejemplar>> ObtenerInventarioCompleto()
        {
            return await _context.Ejemplares
                .Include(e => e.Libro)
                .OrderBy(e => e.Libro.Titulo)
                .ThenBy(e => e.CodigoBarras)
                .ToListAsync();
        }

        #endregion

        #region Historial de Circulación

        public async Task<byte[]> ExportarHistorialCirculacionExcel(DateTime? desde = null, DateTime? hasta = null)
        {
            var prestamos = await ObtenerHistorialCirculacion(desde, hasta);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Historial Circulación");

            // Encabezados
            var headers = new[] { "ID", "Código", "Libro", "Usuario", "RUT", "Curso", "F. Préstamo", "F. Vencimiento", "F. Devolución", "Estado", "Días Prestado" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Datos
            int row = 2;
            foreach (var p in prestamos)
            {
                var diasPrestado = p.FechaDevolucion.HasValue 
                    ? (p.FechaDevolucion.Value - p.FechaPrestamo).Days 
                    : (DateTime.Now - p.FechaPrestamo).Days;

                worksheet.Cell(row, 1).Value = p.Id;
                worksheet.Cell(row, 2).Value = p.Ejemplar?.CodigoBarras ?? "";
                worksheet.Cell(row, 3).Value = p.Ejemplar?.Libro?.Titulo ?? p.Libro?.Titulo ?? "";
                worksheet.Cell(row, 4).Value = p.Usuario?.NombreCompleto ?? "";
                worksheet.Cell(row, 5).Value = p.Usuario?.RUT ?? "";
                worksheet.Cell(row, 6).Value = p.Usuario?.CursoConLetra ?? "";
                worksheet.Cell(row, 7).Value = p.FechaPrestamo.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 8).Value = p.FechaVencimiento.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 9).Value = p.FechaDevolucion?.ToString("dd/MM/yyyy") ?? "Pendiente";
                worksheet.Cell(row, 10).Value = p.Estado;
                worksheet.Cell(row, 11).Value = diasPrestado;

                // Color según estado
                if (p.Estado == "Activo" && p.FechaVencimiento < DateTime.Now)
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                else if (p.Estado == "Devuelto")
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportarHistorialCirculacionPdf(DateTime? desde = null, DateTime? hasta = null)
        {
            var prestamos = await ObtenerHistorialCirculacion(desde, hasta);
            var rangoFechas = desde.HasValue && hasta.HasValue 
                ? $"Período: {desde.Value:dd/MM/yyyy} - {hasta.Value:dd/MM/yyyy}" 
                : "Todos los registros";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => CrearEncabezadoPdf(c, "Historial de Circulación", $"{rangoFechas} | Total: {prestamos.Count} registros"));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Código
                            columns.RelativeColumn(3); // Libro
                            columns.RelativeColumn(2); // Usuario
                            columns.RelativeColumn(1.5f); // Curso
                            columns.RelativeColumn(1.5f); // F. Préstamo
                            columns.RelativeColumn(1.5f); // F. Devolución
                            columns.RelativeColumn(1); // Estado
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Código").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Libro").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Usuario").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Curso").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Préstamo").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Devolución").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Estado").Bold();
                        });

                        // Datos
                        foreach (var p in prestamos)
                        {
                            var bgColor = p.Estado switch
                            {
                                "Devuelto" => Colors.Green.Lighten4,
                                "Activo" when p.FechaVencimiento < DateTime.Now => Colors.Red.Lighten4,
                                "Activo" => Colors.Blue.Lighten4,
                                _ => Colors.White
                            };

                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.Ejemplar?.CodigoBarras ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.Ejemplar?.Libro?.Titulo ?? p.Libro?.Titulo ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.Usuario?.NombreCompleto ?? "");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.Usuario?.CursoConLetra ?? "-");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.FechaPrestamo.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.FechaDevolucion?.ToString("dd/MM/yyyy") ?? "Pendiente");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(p.Estado);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<List<Prestamo>> ObtenerHistorialCirculacion(DateTime? desde, DateTime? hasta)
        {
            var query = _context.Prestamos
                .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                .Include(p => p.Libro)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (desde.HasValue)
                query = query.Where(p => p.FechaPrestamo >= desde.Value);
            
            if (hasta.HasValue)
                query = query.Where(p => p.FechaPrestamo <= hasta.Value.AddDays(1));

            return await query
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync();
        }

        #endregion

        #region Helpers

        private void CrearEncabezadoPdf(IContainer container, string titulo, string subtitulo)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Biblioteca Escolar").FontSize(12).Bold();
                        col.Item().Text(titulo).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(subtitulo).FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9);
                });
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        #endregion
    }
}

