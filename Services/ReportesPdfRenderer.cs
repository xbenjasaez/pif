using BibliotecaVirtualWeb.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BibliotecaVirtualWeb.Services
{
    public class ReportesPdfRenderer
    {
        private static bool _licenseInitialized;

        public ReportesPdfRenderer()
        {
            if (!_licenseInitialized)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                _licenseInitialized = true;
            }
        }

        public byte[] Generar(ReportesDetalladosViewModel modelo, ReportesExportOptions opciones)
        {
            opciones ??= new ReportesExportOptions();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.Size(PageSizes.A4);

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Biblioteca Virtual Web").FontSize(18).SemiBold();
                                column.Item().Text($"Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm}");
                                column.Item().Text($"Solicitado por: {modelo.GeneradoPor ?? "Usuario no identificado"}");
                                var filtrosActivos = $"Periodo: {modelo.Filtro.Periodo} ({modelo.Filtro.FechaInicio:dd/MM/yyyy} - {modelo.Filtro.FechaFin:dd/MM/yyyy})";
                                column.Item().Text(filtrosActivos);
                                var detalleCurso = string.IsNullOrWhiteSpace(modelo.Filtro.CursoSeleccionado) || modelo.Filtro.CursoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                                    ? "Filtro cursos: Todos"
                                    : $"Filtro cursos: {modelo.Filtro.CursoSeleccionado}";
                                column.Item().Text(detalleCurso);
                                var detalleCategoria = string.IsNullOrWhiteSpace(modelo.Filtro.CategoriaSeleccionada) || modelo.Filtro.CategoriaSeleccionada.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                                    ? "Filtro categorías: Todas"
                                    : $"Filtro categorías: {modelo.Filtro.CategoriaSeleccionada}";
                                column.Item().Text(detalleCategoria);
                            });
                            row.ConstantItem(60).AlignRight().Text("PDF").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        if (opciones.IncluirAlertas && modelo.AlertasRapidas.Any())
                        {
                            column.Item().Element(x => RenderAlertas(x, modelo.AlertasRapidas));
                        }

                        if (opciones.IncluirResumen)
                        {
                            column.Item().Element(x => RenderResumen(x, modelo));
                        }

                        if (opciones.IncluirPrestamos)
                        {
                            column.Item().Element(x => RenderPrestamos(x, modelo));
                        }

                        if (opciones.IncluirRankings)
                        {
                            column.Item().Element(x => RenderRankings(x, modelo));
                        }

                        if (opciones.IncluirEstadisticas)
                        {
                            column.Item().Element(x => RenderEstadisticas(x, modelo));
                        }
                    });

                    page.Footer().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor(Colors.Grey.Darken2));
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void RenderAlertas(IContainer container, IEnumerable<string> alertas)
        {
            container.Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Alertas rápidas")
                        .FontSize(16)
                        .SemiBold()
                        .FontColor(Colors.Red.Darken2);
                });
                column.Item().Column(col =>
                {
                    foreach (var alerta in alertas)
                    {
                        col.Item().Border(1).BorderColor(Colors.Red.Lighten2).Padding(10).Text(alerta);
                    }
                });
            });
        }

        private static void RenderResumen(IContainer container, ReportesDetalladosViewModel modelo)
        {
            container.Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Resumen ejecutivo").FontSize(16).SemiBold();
                });
                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Usuarios registrados: {modelo.TotalUsuarios}");
                    col.Item().Text($"Libros catalogados: {modelo.TotalLibros}");
                    col.Item().Text($"Préstamos analizados: {modelo.TotalPrestamos} (Activos: {modelo.PrestamosActivos})");
                    col.Item().Text($"Índice de rotación: {modelo.IndiceRotacion:0.00}");
                    col.Item().Text($"Tasa de préstamos activos: {modelo.TasaPrestamosActivos:0.0}%");
                    col.Item().Text($"Eficiencia de devoluciones: {modelo.EficienciaDevoluciones:0.0}%");
                });
            });
        }

        private static void RenderPrestamos(IContainer container, ReportesDetalladosViewModel modelo)
        {
            container.Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Análisis de préstamos").FontSize(16).SemiBold();
                });
                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Tasa devoluciones puntuales: {modelo.TasaDevolucionPuntual:0.0}% ({modelo.TotalDevolucionesPuntuales} ejemplares)");
                    col.Item().Text($"Tasa devoluciones tardías: {modelo.TasaDevolucionTardia:0.0}% ({modelo.TotalDevolucionesTardias} ejemplares)");
                    col.Item().Text($"Promedio días por préstamo: {modelo.PromedioTiempoPrestamoEnDias:0.0}");
                    col.Item().Text($"Libros no devueltos: {modelo.LibrosNoDevueltos}");
                    col.Item().Text($"Préstamos vencidos: {modelo.PrestamosVencidos}");
                });
            });
        }

        private static void RenderRankings(IContainer container, ReportesDetalladosViewModel modelo)
        {
            container.Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Rankings principales").FontSize(16).SemiBold();
                });
                column.Item().PaddingTop(5).Column(inner =>
                {
                    inner.Item().Text(text =>
                    {
                        text.Span("Top usuarios (máximo 5)").SemiBold();
                    });
                    inner.Item().Element(x => BuildUsuariosTable(x, modelo.TopUsuarios.Take(5)));

                    inner.Item().PaddingTop(8).Text(text =>
                    {
                        text.Span("Top libros (máximo 5)").SemiBold();
                    });
                    inner.Item().Element(x => BuildLibrosTable(x, modelo.TopLibros.Take(5)));
                });
            });
        }

        private static void RenderEstadisticas(IContainer container, ReportesDetalladosViewModel modelo)
        {
            container.Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Estadísticas avanzadas").FontSize(16).SemiBold();
                });
                column.Item().PaddingTop(5).Column(col =>
                {
                    if (modelo.LibroMasSolicitado != null)
                    {
                        col.Item().Text($"Libro más solicitado: {modelo.LibroMasSolicitado.Titulo} ({modelo.LibroMasSolicitado.TotalPrestamos} préstamos)");
                    }
                    if (modelo.CategoriaMasPopular != null)
                    {
                        col.Item().Text($"Categoría más popular: {modelo.CategoriaMasPopular.Categoria} ({modelo.CategoriaMasPopular.TotalPrestamos} préstamos)");
                    }
                    if (modelo.UsuarioMasMoroso != null && modelo.UsuarioMasMoroso.PrestamosVencidos > 0)
                    {
                        col.Item().Text($"Usuario con más atrasos: {modelo.UsuarioMasMoroso.NombreCompleto} ({modelo.UsuarioMasMoroso.PrestamosVencidos} vencidos)");
                    }

                    if (modelo.EstadisticasMensuales.Any())
                    {
                        col.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("Tendencia últimos meses").SemiBold();
                        });
                        col.Item().Element(x => BuildEstadisticasTable(x, modelo.EstadisticasMensuales.TakeLast(6)));
                    }

                    if (modelo.CursosTop.Any())
                    {
                        col.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("Actividad por curso (Top 5)").SemiBold();
                        });
                        col.Item().Element(x => BuildCursosTable(x, modelo.CursosTop.Take(5)));
                    }

                    if (modelo.ChartData != null)
                    {
                        col.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("Resumen visual (datos filtrados)").SemiBold();
                        });
                        col.Item().Element(x => RenderChartSummaries(x, modelo.ChartData));
                    }
                });
            });
        }

        private static void BuildUsuariosTable(IContainer container, IEnumerable<UsuarioTopViewModel> usuarios)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn();
                    columns.ConstantColumn(60);
                });

                table.Header(header =>
                {
                    header.Cell().Text("#").SemiBold();
                    header.Cell().Text("Usuario").SemiBold();
                    header.Cell().Text("Préstamos").SemiBold();
                });

                var index = 1;
                foreach (var usuario in usuarios)
                {
                    table.Cell().Text(index.ToString());
                    table.Cell().Text($"{usuario.NombreCompleto} ({usuario.RUT})");
                    table.Cell().Text(usuario.TotalPrestamos.ToString());
                    index++;
                }
            });
        }

        private static void BuildLibrosTable(IContainer container, IEnumerable<LibroTopViewModel> libros)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn();
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    header.Cell().Text("#").SemiBold();
                    header.Cell().Text("Libro").SemiBold();
                    header.Cell().Text("Préstamos").SemiBold();
                });

                var index = 1;
                foreach (var libro in libros)
                {
                    table.Cell().Text(index.ToString());
                    table.Cell().Text($"{libro.Titulo} - {libro.Autor}");
                    table.Cell().Text(libro.TotalPrestamos.ToString());
                    index++;
                }
            });
        }

        private static void BuildEstadisticasTable(IContainer container, IEnumerable<EstadisticaMensualViewModel> datos)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Mes").SemiBold();
                    header.Cell().Text("Préstamos").SemiBold();
                    header.Cell().Text("Devoluciones").SemiBold();
                    header.Cell().Text("Tasa %").SemiBold();
                    header.Cell().Text("Segmento").SemiBold();
                });

                foreach (var dato in datos)
                {
                    var tasa = dato.TotalPrestamos > 0
                        ? Math.Round(dato.TotalDevoluciones * 100.0 / dato.TotalPrestamos, 1)
                        : 0;

                    table.Cell().Text($"{dato.Mes} {dato.Año}");
                    table.Cell().Text(dato.TotalPrestamos.ToString());
                    table.Cell().Text(dato.TotalDevoluciones.ToString());
                    table.Cell().Text($"{tasa}%");
                    table.Cell().Text(dato.Segmento ?? "Global");
                }
            });
        }

        private static void BuildCursosTable(IContainer container, IEnumerable<CursoActividadViewModel> cursos)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(40);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Curso").SemiBold();
                    header.Cell().Text("Total").SemiBold();
                    header.Cell().Text("%").SemiBold();
                    header.Cell().Text("Vencidos").SemiBold();
                });

                foreach (var curso in cursos)
                {
                    table.Cell().Text(curso.Curso);
                    table.Cell().Text(curso.TotalPrestamos.ToString());
                    table.Cell().Text($"{curso.PorcentajeDelTotal}%");
                    table.Cell().Text(curso.PrestamosVencidos.ToString());
                }
            });
        }

        private static void RenderChartSummaries(IContainer container, ReportesChartViewModel chartData)
        {
            container.Column(column =>
            {
                if (chartData.TendenciaLabels.Any())
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Mes").SemiBold();
                            header.Cell().Text("Total").SemiBold();
                        });

                        for (var i = 0; i < chartData.TendenciaLabels.Count; i++)
                        {
                            table.Cell().Text(chartData.TendenciaLabels[i]);
                            var valor = chartData.TendenciaSeries.FirstOrDefault()?.Valores.ElementAtOrDefault(i) ?? 0;
                            table.Cell().Text(valor.ToString());
                        }
                    });
                }

                if (chartData.CategoriasLabels.Any())
                {
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Categoría").SemiBold();
                            header.Cell().Text("Préstamos").SemiBold();
                        });

                        for (var i = 0; i < chartData.CategoriasLabels.Count; i++)
                        {
                            table.Cell().Text(chartData.CategoriasLabels[i]);
                            table.Cell().Text(chartData.CategoriasValores.ElementAtOrDefault(i).ToString());
                        }
                    });
                }

                if (chartData.EstadoLabels.Any())
                {
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Estado").SemiBold();
                            header.Cell().Text("Cantidad").SemiBold();
                        });

                        for (var i = 0; i < chartData.EstadoLabels.Count; i++)
                        {
                            table.Cell().Text(chartData.EstadoLabels[i]);
                            table.Cell().Text(chartData.EstadoValores.ElementAtOrDefault(i).ToString());
                        }
                    });
                }
            });
        }
    }
}

