# BibliotecaVirtualWeb — Slide Deck (Guion)

## 1. Portada
- BibliotecaVirtualWeb – Plataforma de gestión bibliotecaria.
- Stack: ASP.NET Core 6 MVC · C# · EF Core · MySQL/MariaDB · Razor · Bootstrap 5 · FontAwesome · PowerShell.
- Autor / fecha.

## 2. Arquitectura general
- MVC + capa de servicios; EF Core (Code First, migraciones en `Migrations/`).
- DbContext: `ApplicationDbContext` (Libros, Ejemplares, Usuarios, Proveedores, Prestamos, Auditorias, Alertas, BackupRegistros, Logros, UsuarioLogros).
- Frontend: Razor Views + Bootstrap 5 + JS nativo (fetch/polling), iconos FontAwesome.
- Configuración: `appsettings*.json` (conexión, logging, superusuario, HTTPS opcional).
- Almacenamiento de respaldos en `Backups/` (ContentRoot).

## 3. Seguridad y autenticación
- ASP.NET Identity integrado en el DbContext.
- Roles: Admin requerido para módulos sensibles (p.ej. backups).
- Superusuario parametrizable en `appsettings*.json`.
- HTTPS conmutado por config (`Security.EnforceHttps`).

## 4. Modelo de datos y migraciones
- Migraciones: baseline + Auditoría, Sistema de alerta, Ubicación en Ejemplar, LetraCurso en Usuario, Backups, Gamificación.
- Índices/unicidad: RUT único, CódigoBarras único; índices en Auditoría (fecha, usuario).
- Relacionales: cascada / restrict / set null según entidad (p.ej. Prestamos–Ejemplar/Libro/Usuario, Libro–Proveedor).

## 5. Módulos funcionales (qué hace y cómo)
- **Libros/Ejemplares**: CRUD; estado, ubicación, notas; códigos de barras únicos; relación Libro–Ejemplar.
- **Usuarios**: CRUD; RUT único; tipos (Alumno/otros); contacto; notas.
- **Préstamos**: vincula Ejemplar/Libro/Usuario; estados y fechas; borrados en cascada según reglas.
- **Proveedores**: catálogo con tipo (donación/compra), contacto, email/teléfono.
- **Inventario**: gestión interna; filtros/orden en UI.
- **Catálogo público**: vistas de consulta y detalle.
- **Importación**: `ImportacionController` + `ImportadorService`; validación de estructura.
- **Exportación**: `ExportacionController` + `ExportacionService`; descargas/reportes.
- **Reportes**: `ReportesController` + `ReportesPdfRenderer` (PDF).
- **Auditoría**: `AuditoriaService` registra acción, usuario, IP, fecha; índices para consulta.
- **Alertas de sistema**: `AlertaSistemaService` gestiona incidencias, tipos y resolución.
- **Gamificación**: `GamificationService`; logros presembrados (primer préstamo, 5/10 préstamos, puntualidad); puntos, icono, color.
- **Backups**:
  - UI `Views/Backup/Index.cshtml`: modal, barra de progreso, polling; métricas (conteo, espacio, último backup); limpieza de antiguos.
  - `BackupController`: tarea en background con scope DI; endpoint `/Progreso` (JSON); descarga/eliminación.
  - `BackupService`: intenta `mysqldump` con `--result-file` (timeout 5 min); fallback manual streaming (`StreamWriter`, `SHOW CREATE TABLE`, INSERT por lotes 1000, bajo RAM, progreso 0–100); registra en `BackupRegistros`.
  - Script alternativo: `Scripts/BackupBiblioteca.ps1` (mysqldump, zip opcional).
  - Limpieza: mantiene N últimos (default 10).

## 6. UI/UX
- Bootstrap 5, FontAwesome.
- Tablas responsivas, alerts/badges de estado.
- JS nativo para AJAX/polling (progreso de backup).
- Métricas en tarjetas (total respaldos, espacio, último backup).

## 7. Configuración y despliegue
- `appsettings.json` / `appsettings.Production.json`: cadenas MySQL/MariaDB (o SQLite dev), logging, superusuario, HTTPS.
- Scripts: `IniciarBiblioteca.bat`, `LaunchBiblioteca.ps1`; `IniciarDesarrollo.bat`.
- Backups en `Backups/`; asegurar permisos y espacio.

## 8. Operación y mantenimiento
- Requisito: `mysqldump` accesible; si no, fallback manual.
- Rendimiento: streaming reduce RAM; timeout 5 min en mysqldump para bases grandes.
- Limpieza programable de respaldos (mantener últimos N).
- Revisar roles/HTTPS antes de producción; monitorear auditoría y espacio.

## 9. Tecnologías y librerías
- Lenguaje: C#.
- Frameworks: ASP.NET Core 6, EF Core, Identity, Razor, Bootstrap 5.
- BD: MySQL/MariaDB (SQLite dev posible).
- Scripts: PowerShell, batch.
- PDFs: renderer en `ReportesPdfRenderer` (iText/Similar).
- Íconos: FontAwesome.

## 10. Beneficios y próximos pasos
- Beneficios: trazabilidad (auditoría), resiliencia (doble estrategia de respaldo), UX clara, modularidad (reportes, gamificación).
- Próximos pasos: tareas programadas de backup, 2FA para admins, monitoreo de salud/logs, pruebas de carga en import/export, retención configurable por entorno.

