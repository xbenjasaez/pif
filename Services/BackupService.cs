using System.Diagnostics;
using System.Text;
using System.Data.Common;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Services
{
    public interface IBackupProgress
    {
        string Estado { get; set; }
        int Porcentaje { get; set; }
    }

    public class BackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly string _backupFolder;

        public BackupService(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            
            // Carpeta de backups en el directorio de la aplicación
            _backupFolder = Path.Combine(_environment.ContentRootPath, "Backups");
            
            // Crear carpeta si no existe
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        /// <summary>
        /// Genera un backup completo de la base de datos
        /// </summary>
        public async Task<BackupResult> GenerarBackupAsync(string? descripcion = null, IBackupProgress? progress = null)
        {
            var resultado = new BackupResult();
            
            try
            {
                // Generar nombre único para el backup
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var nombreArchivo = $"biblioteca_backup_{timestamp}.sql";
                var rutaCompleta = Path.Combine(_backupFolder, nombreArchivo);

                // Obtener configuración de conexión
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var (server, database, user, password) = ParseConnectionString(connectionString);

                // Actualizar progreso
                if (progress != null)
                {
                    progress.Porcentaje = 5;
                    progress.Estado = "Intentando usar mysqldump...";
                }

                // Intentar usar mysqldump primero
                var mysqldumpResult = await EjecutarMysqldump(server, database, user, password, rutaCompleta);
                
                if (mysqldumpResult.Success && File.Exists(rutaCompleta) && new FileInfo(rutaCompleta).Length > 0)
                {
                    if (progress != null)
                    {
                        progress.Porcentaje = 100;
                        progress.Estado = "Completado";
                    }
                    
                    resultado.Exitoso = true;
                    resultado.RutaArchivo = rutaCompleta;
                    resultado.NombreArchivo = nombreArchivo;
                    resultado.TamañoBytes = new FileInfo(rutaCompleta).Length;
                    resultado.Mensaje = "Backup generado exitosamente con mysqldump";
                }
                else
                {
                    // Si mysqldump falla o no está disponible, usar método alternativo
                    if (progress != null)
                    {
                        progress.Porcentaje = 10;
                        progress.Estado = "Usando método alternativo...";
                    }
                    
                    if (File.Exists(rutaCompleta))
                    {
                        File.Delete(rutaCompleta);
                    }
                    
                    await GenerarBackupManualAsync(rutaCompleta, progress);
                    
                    if (File.Exists(rutaCompleta))
                    {
                        resultado.Exitoso = true;
                        resultado.RutaArchivo = rutaCompleta;
                        resultado.NombreArchivo = nombreArchivo;
                        resultado.TamañoBytes = new FileInfo(rutaCompleta).Length;
                        resultado.Mensaje = $"Backup generado con método alternativo. {mysqldumpResult.Output}";
                    }
                    else
                    {
                        throw new Exception("No se pudo crear el archivo de backup");
                    }
                }

                // Actualizar progreso antes de registrar
                if (progress != null)
                {
                    progress.Porcentaje = 95;
                    progress.Estado = "Registrando backup...";
                }

                // Registrar el backup en la base de datos
                var registro = new BackupRegistro
                {
                    NombreArchivo = nombreArchivo,
                    RutaCompleta = rutaCompleta,
                    FechaCreacion = DateTime.Now,
                    TamañoBytes = resultado.TamañoBytes,
                    Descripcion = descripcion ?? $"Backup automático {timestamp}",
                    Exitoso = true
                };
                
                _context.BackupRegistros.Add(registro);
                await _context.SaveChangesAsync();
                
                resultado.RegistroId = registro.Id;
                
                // Marcar como completado
                if (progress != null && progress is dynamic)
                {
                    try 
                    { 
                        ((dynamic)progress).Porcentaje = 100; 
                        ((dynamic)progress).Estado = "Completado"; 
                    } 
                    catch { }
                }
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error al generar backup: {ex.Message}";
                resultado.Error = ex.ToString();
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene la lista de backups disponibles
        /// </summary>
        public async Task<List<BackupInfo>> ObtenerBackupsAsync()
        {
            var backups = new List<BackupInfo>();

            // Obtener registros de la BD
            var registros = await _context.BackupRegistros
                .OrderByDescending(b => b.FechaCreacion)
                .ToListAsync();

            foreach (var registro in registros)
            {
                var info = new BackupInfo
                {
                    Id = registro.Id,
                    NombreArchivo = registro.NombreArchivo,
                    FechaCreacion = registro.FechaCreacion,
                    TamañoBytes = registro.TamañoBytes,
                    Descripcion = registro.Descripcion,
                    ExisteArchivo = File.Exists(registro.RutaCompleta)
                };
                backups.Add(info);
            }

            // También buscar archivos que no estén en la BD
            if (Directory.Exists(_backupFolder))
            {
                var archivos = Directory.GetFiles(_backupFolder, "*.sql")
                    .Where(f => !registros.Any(r => r.RutaCompleta == f));

                foreach (var archivo in archivos)
                {
                    var fileInfo = new FileInfo(archivo);
                    backups.Add(new BackupInfo
                    {
                        Id = 0,
                        NombreArchivo = fileInfo.Name,
                        FechaCreacion = fileInfo.CreationTime,
                        TamañoBytes = fileInfo.Length,
                        Descripcion = "Backup sin registro",
                        ExisteArchivo = true
                    });
                }
            }

            return backups.OrderByDescending(b => b.FechaCreacion).ToList();
        }

        /// <summary>
        /// Elimina un backup específico
        /// </summary>
        public async Task<bool> EliminarBackupAsync(int id)
        {
            var registro = await _context.BackupRegistros.FindAsync(id);
            if (registro == null) return false;

            // Eliminar archivo físico
            if (File.Exists(registro.RutaCompleta))
            {
                File.Delete(registro.RutaCompleta);
            }

            // Eliminar registro
            _context.BackupRegistros.Remove(registro);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Obtiene la ruta de un backup para descarga
        /// </summary>
        public async Task<string?> ObtenerRutaBackupAsync(int id)
        {
            var registro = await _context.BackupRegistros.FindAsync(id);
            if (registro == null || !File.Exists(registro.RutaCompleta))
                return null;

            return registro.RutaCompleta;
        }

        /// <summary>
        /// Limpia backups antiguos (mantiene los últimos N)
        /// </summary>
        public async Task<int> LimpiarBackupsAntiguosAsync(int mantenerUltimos = 10)
        {
            var registros = await _context.BackupRegistros
                .OrderByDescending(b => b.FechaCreacion)
                .Skip(mantenerUltimos)
                .ToListAsync();

            var eliminados = 0;
            foreach (var registro in registros)
            {
                if (File.Exists(registro.RutaCompleta))
                {
                    File.Delete(registro.RutaCompleta);
                }
                _context.BackupRegistros.Remove(registro);
                eliminados++;
            }

            await _context.SaveChangesAsync();
            return eliminados;
        }

        #region Métodos privados

        private async Task<(bool Success, string Output)> EjecutarMysqldump(
            string server, string database, string user, string password, string outputPath)
        {
            try
            {
                // Buscar mysqldump en ubicaciones comunes
                var mysqldumpPaths = new[]
                {
                    @"C:\xampp\mysql\bin\mysqldump.exe",
                    @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
                    @"C:\Program Files\MariaDB 10.5\bin\mysqldump.exe",
                    "mysqldump" // PATH del sistema
                };

                string? mysqldumpPath = null;
                foreach (var path in mysqldumpPaths)
                {
                    if (path == "mysqldump" || File.Exists(path))
                    {
                        mysqldumpPath = path;
                        break;
                    }
                }

                if (mysqldumpPath == null)
                {
                    return (false, "mysqldump no encontrado");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = mysqldumpPath,
                    Arguments = $"-h {server} -u {user} -p{password} --single-transaction --routines --triggers {database}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Esperar con timeout de 2 minutos
                var completed = await Task.Run(() => process.WaitForExit(120000));
                
                if (!completed)
                {
                    try { process.Kill(); } catch { }
                    return (false, "Timeout: mysqldump tardó más de 2 minutos");
                }

                if (process.ExitCode == 0 && output.Length > 0)
                {
                    await File.WriteAllTextAsync(outputPath, output.ToString(), Encoding.UTF8);
                    return (true, "OK");
                }

                return (false, error.Length > 0 ? error.ToString() : "mysqldump falló sin mensaje de error");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task GenerarBackupManualAsync(string outputPath, IBackupProgress? progress = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("-- =============================================");
            sb.AppendLine($"-- Backup de Biblioteca Virtual");
            sb.AppendLine($"-- Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("-- =============================================");
            sb.AppendLine();
            sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
            sb.AppendLine();
            sb.AppendLine("-- =============================================");
            sb.AppendLine("-- ESTRUCTURA DE TABLAS (CREATE TABLE)");
            sb.AppendLine("-- =============================================");
            sb.AppendLine();

            // Exportar estructura de tablas
            if (progress != null)
            {
                progress.Porcentaje = 5;
                progress.Estado = "Exportando estructura de tablas...";
            }

            await ExportarEstructuraTablasAsync(sb, progress);

            sb.AppendLine();
            sb.AppendLine("-- =============================================");
            sb.AppendLine("-- DATOS DE TABLAS (INSERT)");
            sb.AppendLine("-- =============================================");
            sb.AppendLine();

            // Exportar datos de todas las tablas
            // Estructura: 0-10%, Datos: 10-90%, Guardar: 90-100%
            await ExportarDatosTodasTablasAsync(sb, progress);

            sb.AppendLine();
            sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
            sb.AppendLine();
            sb.AppendLine("-- Fin del backup");

            if (progress != null)
            {
                progress.Porcentaje = 95;
                progress.Estado = "Escribiendo archivo...";
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
            
            if (progress != null)
            {
                progress.Porcentaje = 100;
                progress.Estado = "Completado";
            }
        }

        private async Task ExportarEstructuraTablasAsync(StringBuilder sb, IBackupProgress? progress)
        {
            // Obtener todas las tablas de la base de datos dinámicamente
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                // Obtener lista de todas las tablas (excluyendo vistas y tablas del sistema)
                var tablas = new List<string>();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT table_name 
                        FROM information_schema.tables
                        WHERE table_schema = DATABASE() 
                        AND table_type = 'BASE TABLE'
                        ORDER BY table_name";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tableName = reader.GetString(0);
                        tablas.Add(tableName);
                    }
                }
                
                var totalTablas = tablas.Count;
                var indice = 0;
                
                // Exportar estructura de cada tabla
                foreach (var tabla in tablas)
                {
                    try
                    {
                        // Actualizar progreso (0% a 10% para estructura)
                        if (progress != null && totalTablas > 0)
                        {
                            var porcentaje = (int)(10.0 * indice / totalTablas);
                            progress.Porcentaje = porcentaje;
                            progress.Estado = $"Exportando estructura: {tabla}...";
                            // Pequeño delay para que el progreso sea visible
                            await Task.Delay(50);
                        }
                        
                        using var command = connection.CreateCommand();
                        command.CommandText = $"SHOW CREATE TABLE `{tabla}`";
                        
                        using var reader = await command.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            // SHOW CREATE TABLE devuelve: Table, Create Table
                            var createTable = reader.GetString(1); // Segunda columna
                            
                            if (!string.IsNullOrEmpty(createTable))
                            {
                                sb.AppendLine($"-- Estructura de la tabla: {tabla}");
                                sb.AppendLine($"DROP TABLE IF EXISTS `{tabla}`;");
                                sb.AppendLine(createTable + ";");
                                sb.AppendLine();
                            }
                        }
                        
                        indice++;
                    }
                    catch
                    {
                        // Si hay error con una tabla, continuar con la siguiente
                        indice++;
                        continue;
                    }
                }
                
                if (progress != null)
                {
                    progress.Porcentaje = 10;
                    progress.Estado = "Estructura exportada, comenzando exportación de datos...";
                }
            }
            finally
            {
                if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task ExportarDatosTodasTablasAsync(StringBuilder sb, IBackupProgress? progress)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                // Obtener lista de todas las tablas
                var tablas = new List<string>();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT table_name 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_type = 'BASE TABLE'
                        ORDER BY table_name";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tableName = reader.GetString(0);
                        tablas.Add(tableName);
                    }
                }
                
                var totalTablas = tablas.Count;
                var indice = 0;
                
                // Exportar datos de cada tabla
                foreach (var tabla in tablas)
                {
                    try
                    {
                        // Calcular progreso (10% a 85%)
                        if (progress != null && totalTablas > 0)
                        {
                            var porcentaje = 10 + (int)(75.0 * indice / totalTablas);
                            progress.Porcentaje = porcentaje;
                            progress.Estado = $"Exportando datos de {tabla} ({indice + 1}/{totalTablas})...";
                        }
                        
                        await ExportarDatosTablaAsync(sb, tabla, connection, progress, indice, totalTablas);
                        indice++;
                    }
                    catch
                    {
                        // Si hay error con una tabla, continuar con la siguiente
                        indice++;
                        continue;
                    }
                }
                
                if (progress != null)
                {
                    progress.Porcentaje = 85;
                    progress.Estado = "Finalizando exportación...";
                }
            }
            finally
            {
                if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task ExportarDatosTablaAsync(StringBuilder sb, string tableName, DbConnection connection, IBackupProgress? progress, int indiceTabla, int totalTablas)
        {
            // Obtener conteo de registros
            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM `{tableName}`";
            var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            
            if (count == 0)
            {
                sb.AppendLine($"-- Tabla: {tableName} (vacía)");
                sb.AppendLine();
                return;
            }
            
            sb.AppendLine($"-- Tabla: {tableName}");
            sb.AppendLine($"-- Registros: {count}");
            
            // Obtener nombres de columnas
            var columnas = new List<string>();
            using (var columnCommand = connection.CreateCommand())
            {
                columnCommand.CommandText = $"SHOW COLUMNS FROM `{tableName}`";
                using var reader = await columnCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columnas.Add(reader.GetString(0)); // Field
                }
            }
            
            if (columnas.Count == 0) return;
            
            var columnasStr = string.Join(", ", columnas.Select(c => $"`{c}`"));
            
            // Exportar datos en lotes para evitar problemas de memoria
            const int batchSize = 1000;
            var offset = 0;
            var totalBatches = (int)Math.Ceiling((double)count / batchSize);
            var batchIndex = 0;
            
            while (offset < count)
            {
                // Actualizar progreso durante el procesamiento de lotes (cada 5 lotes o en el primero)
                if (progress != null && totalTablas > 0 && totalBatches > 0 && (batchIndex == 0 || batchIndex % 5 == 0))
                {
                    // Progreso base de la tabla (10% a 85%)
                    var progresoTablaBase = 10 + (int)(75.0 * indiceTabla / totalTablas);
                    // Progreso dentro de la tabla (0% a 100% de esta tabla)
                    var progresoEnTabla = (int)((double)batchIndex / totalBatches);
                    // Progreso total
                    var progresoPorLote = 75.0 / totalTablas;
                    var porcentaje = progresoTablaBase + (int)(progresoPorLote * progresoEnTabla / 100.0);
                    
                    progress.Porcentaje = Math.Min(porcentaje, 85);
                    progress.Estado = $"Exportando {tableName}: {offset}/{count} registros...";
                    // Pequeño delay para que el progreso sea visible
                    await Task.Delay(100);
                }
                
                using var dataCommand = connection.CreateCommand();
                dataCommand.CommandText = $"SELECT {columnasStr} FROM `{tableName}` LIMIT {batchSize} OFFSET {offset}";
                
                using var reader = await dataCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var valores = new List<string>();
                    
                    for (int i = 0; i < columnas.Count; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            valores.Add("NULL");
                        }
                        else
                        {
                            var valor = reader.GetValue(i);
                            if (valor is string s)
                            {
                                valores.Add($"'{EscapeSql(s)}'");
                            }
                            else if (valor is DateTime dt)
                            {
                                valores.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                            }
                            else if (valor is bool b)
                            {
                                valores.Add(b ? "1" : "0");
                            }
                            else
                            {
                                valores.Add(valor.ToString() ?? "NULL");
                            }
                        }
                    }
                    
                    sb.AppendLine($"INSERT INTO `{tableName}` ({columnasStr}) VALUES ({string.Join(", ", valores)});");
                }
                
                offset += batchSize;
                batchIndex++;
            }
            
            // Actualizar progreso al completar la tabla
            if (progress != null && totalTablas > 0)
            {
                var porcentaje = 10 + (int)(75.0 * (indiceTabla + 1) / totalTablas);
                progress.Porcentaje = Math.Min(porcentaje, 85);
                progress.Estado = $"Completada {tableName} ({indiceTabla + 1}/{totalTablas})...";
                await Task.Delay(50);
            }
            
            sb.AppendLine();
        }

        private async Task ExportarTablaAsync<T>(StringBuilder sb, string tableName, DbSet<T> dbSet) where T : class
        {
            var datos = await dbSet.ToListAsync();
            if (!datos.Any()) return;

            sb.AppendLine($"-- Tabla: {tableName}");
            sb.AppendLine($"-- Registros: {datos.Count}");
            // No se necesita DELETE FROM porque la estructura ya incluye DROP TABLE IF EXISTS

            var propiedades = typeof(T).GetProperties()
                .Where(p => p.CanRead && !p.GetMethod!.IsVirtual)
                .ToList();

            var columnas = string.Join(", ", propiedades.Select(p => $"`{p.Name}`"));

            foreach (var item in datos)
            {
                var valores = propiedades.Select(p =>
                {
                    var valor = p.GetValue(item);
                    if (valor == null) return "NULL";
                    if (valor is string s) return $"'{EscapeSql(s)}'";
                    if (valor is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                    if (valor is bool b) return b ? "1" : "0";
                    return valor.ToString();
                });

                sb.AppendLine($"INSERT INTO `{tableName}` ({columnas}) VALUES ({string.Join(", ", valores)});");
            }

            sb.AppendLine();
        }

        private string EscapeSql(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        private (string Server, string Database, string User, string Password) ParseConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string no configurada");

            var parts = connectionString.Split(';')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim());

            return (
                parts.GetValueOrDefault("server", "localhost"),
                parts.GetValueOrDefault("database", "biblioteca_virtual"),
                parts.GetValueOrDefault("user", "root"),
                parts.GetValueOrDefault("password", "")
            );
        }

        #endregion
    }

    #region Modelos

    public class BackupResult
    {
        public bool Exitoso { get; set; }
        public string? RutaArchivo { get; set; }
        public string? NombreArchivo { get; set; }
        public long TamañoBytes { get; set; }
        public string? Mensaje { get; set; }
        public string? Error { get; set; }
        public int RegistroId { get; set; }
    }

    public class BackupInfo
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public long TamañoBytes { get; set; }
        public string? Descripcion { get; set; }
        public bool ExisteArchivo { get; set; }
        
        public string TamañoFormateado
        {
            get
            {
                if (TamañoBytes < 1024) return $"{TamañoBytes} B";
                if (TamañoBytes < 1024 * 1024) return $"{TamañoBytes / 1024.0:F1} KB";
                return $"{TamañoBytes / (1024.0 * 1024.0):F1} MB";
            }
        }
    }

    #endregion
}

