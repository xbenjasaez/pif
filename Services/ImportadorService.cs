using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BibliotecaVirtualWeb.Services
{
    public class ImportadorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImportadorService> _logger;

        public ImportadorService(ApplicationDbContext context, ILogger<ImportadorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Importación de Alumnos

        public async Task<ResultadoImportacion> ImportarAlumnosAsync(Stream csvStream, bool soloValidar = true)
        {
            var resultado = new ResultadoImportacion { TipoImportacion = "Alumnos" };
            var lineas = await LeerCsvAsync(csvStream, ';');

            if (lineas.Count < 2)
            {
                resultado.Errores.Add("El archivo CSV está vacío o no tiene datos.");
                return resultado;
            }

            // Obtener RUTs existentes para detectar duplicados
            var usuariosExistentes = await _context.Usuarios
                .Select(u => u.RUT)
                .ToListAsync();
            var rutsExistentesSet = new HashSet<string>(usuariosExistentes.Select(r => NormalizarRut(r)));

            // Saltar encabezado
            for (int i = 1; i < lineas.Count; i++)
            {
                var campos = lineas[i];
                if (campos.Length < 7 || string.IsNullOrWhiteSpace(campos[1]))
                    continue;

                try
                {
                    var descGrado = campos[0].Trim();
                    var run = campos[1].Trim();
                    var digitoVerificador = campos[2].Trim().ToUpperInvariant();
                    var genero = campos[3].Trim().ToUpperInvariant();
                    var nombres = NormalizarNombre(campos[4].Trim());
                    var apellidoPaterno = NormalizarNombre(campos[5].Trim());
                    var apellidoMaterno = NormalizarNombre(campos[6].Trim());

                    // Construir RUT formateado
                    var rutCompleto = FormatearRut(run, digitoVerificador);
                    var rutNormalizado = NormalizarRut(rutCompleto);

                    // Validaciones
                    var erroresLinea = new List<string>();

                    if (string.IsNullOrWhiteSpace(nombres))
                        erroresLinea.Add("Nombre vacío");

                    if (string.IsNullOrWhiteSpace(apellidoPaterno))
                        erroresLinea.Add("Apellido paterno vacío");

                    if (!ValidarRut(run, digitoVerificador))
                        erroresLinea.Add($"RUT inválido: {rutCompleto}");

                    if (!string.IsNullOrEmpty(genero) && genero != "F" && genero != "M")
                        erroresLinea.Add($"Género inválido: {genero}");

                    var curso = NormalizarCurso(descGrado);
                    if (string.IsNullOrWhiteSpace(curso))
                        erroresLinea.Add($"Curso no reconocido: {descGrado}");

                    if (erroresLinea.Any())
                    {
                        resultado.Errores.Add($"Línea {i + 1}: {string.Join(", ", erroresLinea)}");
                        continue;
                    }

                    // Verificar si ya existe
                    var esNuevo = !rutsExistentesSet.Contains(rutNormalizado);

                    var usuario = new Usuario
                    {
                        Nombre = nombres,
                        Apellido = $"{apellidoPaterno} {apellidoMaterno}".Trim(),
                        RUT = rutCompleto,
                        Genero = genero,
                        Curso = curso,
                        LetraCurso = "A", // Por defecto, el CSV no tiene letra asi que asumo que es A
                        Estado = "Activo",
                        FechaRegistro = DateTime.Now
                    };

                    if (esNuevo)
                    {
                        resultado.NuevosRegistros.Add(new RegistroImportacion
                        {
                            Descripcion = $"{usuario.NombreCompleto} - {usuario.RUT} - {usuario.CursoConLetra}",
                            Entidad = usuario
                        });
                        rutsExistentesSet.Add(rutNormalizado);
                    }
                    else
                    {
                        resultado.ActualizacionesPendientes.Add(new RegistroImportacion
                        {
                            Descripcion = $"{usuario.NombreCompleto} - {usuario.RUT} - {usuario.CursoConLetra}",
                            Entidad = usuario
                        });
                    }
                }
                catch (Exception ex)
                {
                    resultado.Errores.Add($"Línea {i + 1}: Error procesando - {ex.Message}");
                }
            }

            // Si no es solo validación, aplicar cambios
            if (!soloValidar && (resultado.NuevosRegistros.Any() || resultado.ActualizacionesPendientes.Any()))
            {
                await AplicarCambiosAlumnosAsync(resultado);
            }

            return resultado;
        }

        private async Task AplicarCambiosAlumnosAsync(ResultadoImportacion resultado)
        {
            //  para manejar reintentos con transacciones
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cargar todos los usuarios existentes para comparar
                    var usuariosExistentes = await _context.Usuarios.ToListAsync();
                    
                    // Insertar nuevos usuarios del csv
                    foreach (var registro in resultado.NuevosRegistros)
                    {
                        if (registro.Entidad is Usuario usuario)
                        {
                            _context.Usuarios.Add(usuario);
                        }
                    }

                    // Actualizar usuarios existentes del csv
                    foreach (var registro in resultado.ActualizacionesPendientes)
                    {
                        if (registro.Entidad is Usuario usuarioNuevo)
                        {
                            var rutNormalizado = NormalizarRut(usuarioNuevo.RUT);
                            var usuarioExistente = usuariosExistentes
                                .FirstOrDefault(u => NormalizarRut(u.RUT) == rutNormalizado);

                            if (usuarioExistente != null)
                            {
                                usuarioExistente.Nombre = usuarioNuevo.Nombre;
                                usuarioExistente.Apellido = usuarioNuevo.Apellido;
                                usuarioExistente.Curso = usuarioNuevo.Curso;
                                usuarioExistente.LetraCurso = usuarioNuevo.LetraCurso;
                                usuarioExistente.Genero = usuarioNuevo.Genero;
                                // No cambiar estado ni préstamos
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    resultado.Aplicado = true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    resultado.Errores.Add($"Error al aplicar cambios: {ex.Message}");
                    _logger.LogError(ex, "Error al aplicar cambios de importación de alumnos");
                }
            });
        }

        #endregion

        #region Importación de Libros

        public async Task<ResultadoImportacion> ImportarLibrosAsync(Stream csvStream, bool soloValidar = true)
        {
            var resultado = new ResultadoImportacion { TipoImportacion = "Libros" };
            var lineas = await LeerCsvAsync(csvStream, ';');

            if (lineas.Count < 2)
            {
                resultado.Errores.Add("El archivo CSV está vacío o no tiene datos.");
                return resultado;
            }

            // Buscar la línea de encabezados (contiene "Nombre" y "Autor")
            int indiceEncabezado = -1;
            for (int i = 0; i < Math.Min(10, lineas.Count); i++)
            {
                var linea = string.Join(";", lineas[i]).ToLowerInvariant();
                if (linea.Contains("nombre") && linea.Contains("autor"))
                {
                    indiceEncabezado = i;
                    break;
                }
            }

            if (indiceEncabezado < 0)
            {
                resultado.Errores.Add("No se encontró la línea de encabezados (debe contener 'Nombre' y 'Autor').");
                return resultado;
            }

            // Obtener libros existentes para detectar duplicados
            var librosExistentes = await _context.Libros
                .Select(l => new { l.ISBN, l.Titulo, l.Autor, l.Editorial })
                .ToListAsync();
            
            // Set de claves completas existentes (Título + Autor + Editorial)
            var librosExistentesSet = new HashSet<string>(
                librosExistentes.Select(l => GenerarClaveLibroCompleta(l.Titulo, l.Autor, l.Editorial ?? "")));
            
            // Set temporal para detectar duplicados dentro del mismo CSV
            var librosEnCsvSet = new HashSet<string>();

            // Procesar datos después del encabezado
            for (int i = indiceEncabezado + 1; i < lineas.Count; i++)
            {
                var campos = lineas[i];

                // Buscar la columna del nombre (puede variar según el formato)
                // Formato esperado: R;Nombre;Autor;Editorial;ISBN;Cant.;Estado;Observación
                int colNombre = 2, colAutor = 3, colEditorial = 4, colIsbn = 5, colCantidad = 6, colEstado = 7, colObservacion = 8;

                // Si la primera columna está vacía, ajustar índices
                if (campos.Length > 2 && string.IsNullOrWhiteSpace(campos[0]) && string.IsNullOrWhiteSpace(campos[1]))
                {
                    // El formato tiene columnas vacías al inicio
                }
                else if (campos.Length > 1 && string.IsNullOrWhiteSpace(campos[0]))
                {
                    colNombre = 1;
                    colAutor = 2;
                    colEditorial = 3;
                    colIsbn = 4;
                    colCantidad = 5;
                    colEstado = 6;
                    colObservacion = 7;
                }

                if (campos.Length <= colNombre || string.IsNullOrWhiteSpace(campos[colNombre]))
                    continue;

                try
                {
                    var nombre = campos.Length > colNombre ? NormalizarTitulo(campos[colNombre].Trim()) : "";
                    var autor = campos.Length > colAutor ? NormalizarNombre(campos[colAutor].Trim()) : "";
                    var editorial = campos.Length > colEditorial ? campos[colEditorial].Trim() : "";
                    var isbn = campos.Length > colIsbn ? NormalizarIsbn(campos[colIsbn].Trim()) : "";
                    var cantidadStr = campos.Length > colCantidad ? campos[colCantidad].Trim() : "1";
                    var estadoCsv = campos.Length > colEstado ? campos[colEstado].Trim() : "Bueno";
                    var observacion = campos.Length > colObservacion ? campos[colObservacion].Trim() : "";

                    if (string.IsNullOrWhiteSpace(nombre))
                        continue;

                    // Parsear cantidad
                    if (!int.TryParse(cantidadStr, out int cantidad) || cantidad < 1)
                        cantidad = 1;

                    // Mapear estado
                    var (estado, notaEstado) = MapearEstadoLibro(estadoCsv);

                    // Combinar notas
                    var notas = string.IsNullOrWhiteSpace(notaEstado)
                        ? observacion
                        : string.IsNullOrWhiteSpace(observacion)
                            ? notaEstado
                            : $"{notaEstado}. {observacion}";

                    // Si no tiene autor, poner "Desconocido"
                    if (string.IsNullOrWhiteSpace(autor))
                        autor = "Desconocido";

                    // Normalizar editorial para evitar duplicados por mayúsculas/minúsculas
                    editorial = NormalizarEditorial(editorial);

                    var libro = new Libro
                    {
                        Titulo = nombre,
                        Autor = autor,
                        Editorial = editorial,
                        ISBN = isbn,
                        Estado = "Disponible",
                        Notas = notas,
                        FechaAgregado = DateTime.Now
                    };

                    // Generar clave única: Título + Autor + Editorial (normalizado)
                    var claveLibro = GenerarClaveLibroCompleta(nombre, autor, editorial);
                    
                    // Verificar si ya existe en BD o en el CSV actual
                    bool existeEnBD = librosExistentesSet.Contains(claveLibro);
                    bool existeEnCsv = librosEnCsvSet.Contains(claveLibro);

                    var registroInfo = new LibroConEjemplares
                    {
                        Libro = libro,
                        CantidadEjemplares = cantidad,
                        EstadoEjemplares = estado,
                        NotasEjemplares = notas
                    };

                    if (existeEnBD)
                    {
                        // Ya existe en la BD → agregar ejemplares
                        resultado.ActualizacionesPendientes.Add(new RegistroImportacion
                        {
                            Descripcion = $"{libro.Titulo} - {libro.Autor} [{editorial}] ({cantidad} ejemplar(es) adicionales) - Estado: {estado}",
                            Entidad = registroInfo
                        });
                    }
                    else if (existeEnCsv)
                    {
                        // Ya apareció antes en el CSV → solo sumar ejemplares al registro existente
                        var registroExistente = resultado.NuevosRegistros
                            .FirstOrDefault(r => r.Entidad is LibroConEjemplares le && 
                                GenerarClaveLibroCompleta(le.Libro.Titulo, le.Libro.Autor, le.Libro.Editorial ?? "") == claveLibro);
                        
                        if (registroExistente?.Entidad is LibroConEjemplares leExistente)
                        {
                            leExistente.CantidadEjemplares += cantidad;
                            registroExistente.Descripcion = $"{leExistente.Libro.Titulo} - {leExistente.Libro.Autor} [{leExistente.Libro.Editorial}] ({leExistente.CantidadEjemplares} ejemplar(es)) - Estado: {leExistente.EstadoEjemplares}";
                        }
                    }
                    else
                    {
                        // Es nuevo
                        resultado.NuevosRegistros.Add(new RegistroImportacion
                        {
                            Descripcion = $"{libro.Titulo} - {libro.Autor} [{editorial}] ({cantidad} ejemplar(es)) - Estado: {estado}",
                            Entidad = registroInfo
                        });

                        // Marcar como procesado
                        librosEnCsvSet.Add(claveLibro);
                        librosExistentesSet.Add(claveLibro);
                    }
                }
                catch (Exception ex)
                {
                    resultado.Errores.Add($"Línea {i + 1}: Error procesando - {ex.Message}");
                }
            }

            // Si no es solo validación, aplicar cambios
            if (!soloValidar && (resultado.NuevosRegistros.Any() || resultado.ActualizacionesPendientes.Any()))
            {
                await AplicarCambiosLibrosAsync(resultado);
            }

            return resultado;
        }

        private async Task AplicarCambiosLibrosAsync(ResultadoImportacion resultado)
        {
            // Usar la estrategia de ejecución para manejar reintentos con transacciones
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Lista para guardar ejemplares nuevos y asignarles código después
                    var ejemplaresNuevos = new List<Ejemplar>();
                    
                    // Insertar nuevos libros y ejemplares
                    foreach (var registro in resultado.NuevosRegistros)
                    {
                        if (registro.Entidad is LibroConEjemplares info)
                        {
                            _context.Libros.Add(info.Libro);
                            await _context.SaveChangesAsync(); // Para obtener el ID del libro

                            // Crear ejemplares con código temporal
                            for (int j = 0; j < info.CantidadEjemplares; j++)
                            {
                                var ejemplar = new Ejemplar
                                {
                                    LibroId = info.Libro.Id,
                                    Estado = info.EstadoEjemplares,
                                    Notas = info.NotasEjemplares,
                                    FechaAgregado = DateTime.Now,
                                    CodigoBarras = $"TEMP_{Guid.NewGuid():N}".Substring(0, 13) // Código temporal
                                };
                                _context.Ejemplares.Add(ejemplar);
                                ejemplaresNuevos.Add(ejemplar);
                            }
                        }
                    }

                    // Para libros existentes, solo agregar ejemplares adicionales
                    // Primero cargar todos los libros a memoria para comparar
                    var todosLosLibros = await _context.Libros.ToListAsync();
                    
                    foreach (var registro in resultado.ActualizacionesPendientes)
                    {
                        if (registro.Entidad is LibroConEjemplares info)
                        {
                            // Buscar por Título + Autor + Editorial (normalizado)
                            var claveLibro = GenerarClaveLibroCompleta(
                                info.Libro.Titulo, 
                                info.Libro.Autor, 
                                info.Libro.Editorial ?? "");
                            
                            var libroExistente = todosLosLibros
                                .FirstOrDefault(l => GenerarClaveLibroCompleta(l.Titulo, l.Autor, l.Editorial ?? "") == claveLibro);

                            if (libroExistente != null)
                            {
                                // Agregar ejemplares al libro existente
                                for (int j = 0; j < info.CantidadEjemplares; j++)
                                {
                                    var ejemplar = new Ejemplar
                                    {
                                        LibroId = libroExistente.Id,
                                        Estado = info.EstadoEjemplares,
                                        Notas = info.NotasEjemplares,
                                        FechaAgregado = DateTime.Now,
                                        CodigoBarras = $"TEMP_{Guid.NewGuid():N}".Substring(0, 13) // Código temporal
                                    };
                                    _context.Ejemplares.Add(ejemplar);
                                    ejemplaresNuevos.Add(ejemplar);
                                }
                            }
                        }
                    }

                    // Guardar para obtener los IDs de los ejemplares
                    await _context.SaveChangesAsync();

                    // Ahora actualizar los códigos de barras con el formato EAN13 correcto
                    foreach (var ejemplar in ejemplaresNuevos)
                    {
                        ejemplar.CodigoBarras = GenerarCodigoBarrasEan13(ejemplar.Id);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    resultado.Aplicado = true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    resultado.Errores.Add($"Error al aplicar cambios: {ex.Message}");
                    _logger.LogError(ex, "Error al aplicar cambios de importación de libros");
                }
            });
        }

        #endregion

        #region Helpers

        private async Task<List<string[]>> LeerCsvAsync(Stream stream, char separador)
        {
            var lineas = new List<string[]>();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? linea;
            while ((linea = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(linea))
                {
                    lineas.Add(linea.Split(separador));
                }
            }

            return lineas;
        }

        private string NormalizarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "";

            // Capitalizar cada palabra
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(nombre.ToLowerInvariant());
        }

        private string NormalizarTitulo(string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                return "";

            // Primera letra mayúscula, resto como viene
            titulo = titulo.Trim();
            if (titulo.Length > 0)
            {
                return char.ToUpperInvariant(titulo[0]) + titulo.Substring(1);
            }
            return titulo;
        }

        private string GenerarClaveTituloAutor(string titulo, string autor)
        {
            // Normalizar para comparación: minúsculas, sin acentos, sin espacios extra
            var tituloNorm = NormalizarParaComparacion(titulo);
            var autorNorm = NormalizarParaComparacion(autor);
            return $"{tituloNorm}|{autorNorm}";
        }

        private string GenerarClaveLibroCompleta(string titulo, string autor, string editorial)
        {
            // Clave única: Título + Autor + Editorial (todo normalizado)
            var tituloNorm = NormalizarParaComparacion(titulo);
            var autorNorm = NormalizarParaComparacion(autor);
            var editorialNorm = NormalizarParaComparacion(editorial);
            return $"{tituloNorm}|{autorNorm}|{editorialNorm}";
        }

        private string NormalizarEditorial(string editorial)
        {
            if (string.IsNullOrWhiteSpace(editorial))
                return "";

            // Capitalizar primera letra de cada palabra
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            editorial = textInfo.ToTitleCase(editorial.ToLowerInvariant().Trim());
            
            // Limpiar espacios múltiples
            editorial = Regex.Replace(editorial, @"\s+", " ");
            
            return editorial.Trim();
        }

        private string NormalizarParaComparacion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            // Convertir a minúsculas
            texto = texto.ToLowerInvariant().Trim();
            
            // Quitar acentos
            texto = RemoverAcentos(texto);
            
            // Quitar caracteres especiales y espacios múltiples
            texto = Regex.Replace(texto, @"[^a-z0-9\s]", "");
            texto = Regex.Replace(texto, @"\s+", " ");
            
            return texto.Trim();
        }

        private string RemoverAcentos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private string FormatearRut(string run, string dv)
        {
            // Limpiar run de puntos y guiones
            run = Regex.Replace(run, @"[^\d]", "");

            if (string.IsNullOrWhiteSpace(run))
                return "";

            // Formatear con puntos y guión
            var sb = new StringBuilder();
            var reversed = run.Reverse().ToArray();

            for (int i = 0; i < reversed.Length; i++)
            {
                if (i > 0 && i % 3 == 0)
                    sb.Insert(0, '.');
                sb.Insert(0, reversed[i]);
            }

            return $"{sb}-{dv}";
        }

        private string NormalizarRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut))
                return "";

            // Quitar puntos, guiones y espacios
            return Regex.Replace(rut, @"[^\dkK]", "").ToUpperInvariant();
        }

        private bool ValidarRut(string run, string dv)
        {
            run = Regex.Replace(run, @"[^\d]", "");
            if (string.IsNullOrWhiteSpace(run) || run.Length < 7)
                return false;

            // Calcular dígito verificador
            int suma = 0;
            int multiplicador = 2;

            for (int i = run.Length - 1; i >= 0; i--)
            {
                suma += (run[i] - '0') * multiplicador;
                multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
            }

            int resto = suma % 11;
            string dvCalculado = resto switch
            {
                0 => "0",
                1 => "K",
                _ => (11 - resto).ToString()
            };

            return dvCalculado == dv.ToUpperInvariant();
        }

        private string NormalizarCurso(string descGrado)
        {
            if (string.IsNullOrWhiteSpace(descGrado))
                return "";

            descGrado = descGrado.ToLowerInvariant().Trim();

            // Mapeo de cursos
            var mapeo = new Dictionary<string, string>
            {
                { "prekínder", "Prekínder" },
                { "prekinder", "Prekínder" },
                { "pre-kínder", "Prekínder" },
                { "pre-kinder", "Prekínder" },
                { "kínder", "Kínder" },
                { "kinder", "Kínder" },
                { "1° básico", "1° Básico" },
                { "1 básico", "1° Básico" },
                { "1º básico", "1° Básico" },
                { "2° básico", "2° Básico" },
                { "2 básico", "2° Básico" },
                { "2º básico", "2° Básico" },
                { "3° básico", "3° Básico" },
                { "3 básico", "3° Básico" },
                { "3º básico", "3° Básico" },
                { "4° básico", "4° Básico" },
                { "4 básico", "4° Básico" },
                { "4º básico", "4° Básico" },
                { "5° básico", "5° Básico" },
                { "5 básico", "5° Básico" },
                { "5º básico", "5° Básico" },
                { "6° básico", "6° Básico" },
                { "6 básico", "6° Básico" },
                { "6º básico", "6° Básico" },
                { "7° básico", "7° Básico" },
                { "7 básico", "7° Básico" },
                { "7º básico", "7° Básico" },
                { "8° básico", "8° Básico" },
                { "8 básico", "8° Básico" },
                { "8º básico", "8° Básico" },
                { "1° medio", "1° Medio" },
                { "1 medio", "1° Medio" },
                { "1º medio", "1° Medio" },
                { "2° medio", "2° Medio" },
                { "2 medio", "2° Medio" },
                { "2º medio", "2° Medio" },
                { "3° medio", "3° Medio" },
                { "3 medio", "3° Medio" },
                { "3º medio", "3° Medio" },
                { "4° medio", "4° Medio" },
                { "4 medio", "4° Medio" },
                { "4º medio", "4° Medio" },
            };

            foreach (var kvp in mapeo)
            {
                if (descGrado.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "";
        }

        private string NormalizarIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return "";

            // Quitar guiones y espacios
            return Regex.Replace(isbn, @"[\s\-]", "");
        }

        private (string estado, string nota) MapearEstadoLibro(string estadoCsv)
        {
            if (string.IsNullOrWhiteSpace(estadoCsv))
                return ("Disponible", "");

            var estadoLower = estadoCsv.ToLowerInvariant().Trim();

            return estadoLower switch
            {
                "bueno" => ("Disponible", ""),
                "nuevo" => ("Disponible", ""),
                "buen estado" => ("Disponible", ""),
                "deteriorado" => ("Deteriorado", "Deteriorado"),
                "malo" => ("Deteriorado", "Mal estado"),
                "mal estado" => ("Deteriorado", "Mal estado"),
                "hojas sueltas" => ("Deteriorado", "Hojas sueltas"),
                "sucio" => ("Deteriorado", "Sucio"),
                "faltan hojas" => ("Deteriorado", "Faltan hojas"),
                "tapa dañada" => ("Deteriorado", "Tapa dañada"),
                "dado de baja" => ("Dado de baja", "Dado de baja"),
                "extraviado" => ("Extraviado", "Extraviado"),
                _ => ("Disponible", estadoCsv) // Guardar el valor original como nota
            };
        }

        private string GenerarCodigoBarrasEan13(int id)
        {
            // Prefijo para ejemplares: 200
            var codigo = $"200{id:D9}";

            // Calcular dígito de control EAN-13
            int suma = 0;
            for (int i = 0; i < 12; i++)
            {
                int digito = codigo[i] - '0';
                suma += (i % 2 == 0) ? digito : digito * 3;
            }

            int digitoControl = (10 - (suma % 10)) % 10;
            return codigo + digitoControl;
        }

        #endregion
    }

    #region Modelos de resultado

    public class ResultadoImportacion
    {
        public string TipoImportacion { get; set; } = "";
        public List<RegistroImportacion> NuevosRegistros { get; set; } = new();
        public List<RegistroImportacion> ActualizacionesPendientes { get; set; } = new();
        public List<string> Errores { get; set; } = new();
        public bool Aplicado { get; set; } = false;

        public int TotalNuevos => NuevosRegistros.Count;
        public int TotalActualizaciones => ActualizacionesPendientes.Count;
        public int TotalErrores => Errores.Count;
        public bool TieneErrores => Errores.Any();
    }

    public class RegistroImportacion
    {
        public string Descripcion { get; set; } = "";
        public object? Entidad { get; set; }
    }

    public class LibroConEjemplares
    {
        public Libro Libro { get; set; } = null!;
        public int CantidadEjemplares { get; set; } = 1;
        public string EstadoEjemplares { get; set; } = "Disponible";
        public string? NotasEjemplares { get; set; }
    }

    #endregion
}

