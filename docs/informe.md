# BibliotecaVirtualWeb — Informe Técnico Completo

## 1. Introducción
BibliotecaVirtualWeb es una aplicación web de gestión bibliotecaria que cubre catálogo, préstamos, proveedores, usuarios, auditoría, alertas, gamificación y respaldo de datos. Utiliza ASP.NET Core 6 MVC con C#, Entity Framework Core y MySQL/MariaDB; la interfaz se construye con Razor, Bootstrap 5 y JS ligero.

## 2. Arquitectura
- Patrón MVC con capa de servicios.
- ORM: EF Core (Code First, migraciones en `Migrations/`).
- DbContext: `ApplicationDbContext` con DbSets para Libros, Ejemplares, Usuarios, Proveedores, Prestamos, Auditorias, Alertas, BackupRegistros, Logros, UsuarioLogros.
- Frontend: Razor Views; Bootstrap 5; FontAwesome; JS nativo (fetch/polling).
- Configuración: `appsettings*.json` (conexión, logging, superusuario, HTTPS opcional).
- Almacenamiento de respaldos: carpeta `Backups/` en `ContentRootPath`.
- Scripts auxiliares: PowerShell / batch para operación y desarrollo.

## 3. Seguridad y autenticación
- ASP.NET Identity integrado en el DbContext.
- Roles: Admin requerido para módulos críticos (ej. backups).
- Superusuario parametrizable en configuración.
- HTTPS conmutable (`Security.EnforceHttps`).

## 4. Modelo de datos y migraciones
- Migraciones: baseline + Auditoría, Sistema de alerta, Ubicación (Ejemplar), LetraCurso (Usuario), Backups, Gamificación.
- Índices y unicidad: RUT único en Usuario; CódigoBarras único en Ejemplar; índices en Auditoría (fecha, usuario).
- Relaciones y eliminación: cascada/restrict/set-null según entidad (Prestamo→Ejemplar/Libro/Usuario; Libro→Proveedor).

## 5. Módulos y funcionalidades
- **Libros y Ejemplares**: CRUD; estado, ubicación, notas; códigos de barras únicos; relación Libro–Ejemplar.
- **Usuarios**: CRUD; RUT único; tipos (Alumno/otros); contacto; notas.
- **Préstamos**: vincula Ejemplar/Libro/Usuario; controla estado y fechas; borrados en cascada según configuración.
- **Proveedores**: catálogo con tipo (donación/compra), contacto, email/teléfono.
- **Inventario**: vista administrativa para gestión interna (filtros/orden).
- **Catálogo público**: vistas de consulta y detalle.
- **Importación**: `ImportacionController` + `ImportadorService`; validación de estructura de datos.
- **Exportación**: `ExportacionController` + `ExportacionService`; descargas/reportes.
- **Reportes**: `ReportesController` + `ReportesPdfRenderer` (PDF).
- **Auditoría**: `AuditoriaService` registra acción, usuario, IP, fecha; consultas optimizadas por índices.
- **Alertas de sistema**: `AlertaSistemaService` gestiona incidencias, tipos y estado de resolución.
- **Gamificación**: `GamificationService`; entidades `Logro` y `UsuarioLogro`; logros presembrados (primer préstamo, 5/10 préstamos, puntualidad); puntos, icono, color.
- **Backups** (detalle operativo):
  - UI en `Views/Backup/Index.cshtml`: creación con modal, barra de progreso, polling; métricas (conteo, espacio usado, último respaldo); limpieza de antiguos.
  - `BackupController`: arranca tarea en background con scope DI; endpoint `/Progreso` JSON para polling; descarga y eliminación.
  - `BackupService`:
    - Intenta `mysqldump` (rutas comunes o PATH) con `--result-file`, timeout 5 min; escribe directo a disco.
    - Fallback manual streaming: `StreamWriter`, `SHOW CREATE TABLE`, INSERT por lotes (1000), control de progreso 0–100, bajo uso de memoria.
    - Registro en `BackupRegistros`: nombre, ruta, tamaño, fecha, descripción, éxito.
  - Limpieza: mantiene N más recientes (default 10).
  - Script alterno: `Scripts/BackupBiblioteca.ps1` (mysqldump, compresión opcional).
- **UI/UX**: Bootstrap 5, FontAwesome; tablas responsivas; alerts/badges de estado; métricas en tarjetas; JS nativo para AJAX/polling (progreso de backup).

## 6. Configuración y despliegue
- `appsettings.json` / `appsettings.Production.json`: cadenas MySQL/MariaDB (o SQLite dev), logging, superusuario, HTTPS.
- Scripts: `IniciarBiblioteca.bat`, `LaunchBiblioteca.ps1`; `IniciarDesarrollo.bat`.
- Backups en `Backups/`; asegurar permisos y espacio en disco.

## 7. Operación y mantenimiento
- Requisito: `mysqldump` accesible; si no, se usa el método manual.
- Rendimiento: streaming en el respaldo manual reduce RAM; timeout extendido en mysqldump para bases grandes.
- Limpieza programable de respaldos (mantener últimos N).
- Revisar roles/HTTPS antes de producción; auditar logs; monitorear espacio y salud de la BD.

## 8. Tecnologías
- Lenguaje: C#.
- Frameworks: ASP.NET Core 6, EF Core, Identity, Razor, Bootstrap 5.
- BD: MySQL/MariaDB (SQLite para desarrollo posible).
- Scripts: PowerShell, batch.
- PDFs: renderer en `ReportesPdfRenderer` (iText/Similar).
- Íconos: FontAwesome.

## 9. Beneficios y próximos pasos
- Beneficios: trazabilidad (auditoría), resiliencia (doble estrategia de respaldo), UX clara, modularidad (reportes, gamificación).
- Próximos pasos: tareas programadas de backup, 2FA para admins, monitoreo de salud/logs centralizados, pruebas de carga en import/export, retención configurable por entorno.

