using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class AddGamification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo intenta renombrar si la columna con tilde existe (evita fallar en DBs creadas con el script que ya usa TamanoBytes).
            migrationBuilder.Sql(@"
SET @rename_stmt := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'backup_registros'
              AND column_name = 'TamañoBytes' COLLATE utf8mb4_bin
        ),
        'ALTER TABLE `backup_registros` CHANGE COLUMN `TamañoBytes` `TamanoBytes` bigint NOT NULL',
        'SELECT 0'
    )
);
PREPARE stmt FROM @rename_stmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.AlterColumn<string>(
                name: "LetraCurso",
                table: "Usuarios",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // Agregar TipoUsuario solo si no existe (evita duplicado en entornos ya ajustados).
            migrationBuilder.Sql(@"
SET @add_tipo_usuario := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'Usuarios'
              AND column_name = 'TipoUsuario' COLLATE utf8mb4_bin
        ),
        'SELECT 0',
        'ALTER TABLE `Usuarios` ADD `TipoUsuario` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''Alumno'''
    )
);
PREPARE stmt FROM @add_tipo_usuario;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            // Crear Logros solo si no existe
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `Logros` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Descripcion` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Icono` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Color` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `CodigoInterno` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Puntos` int NOT NULL,
    CONSTRAINT `PK_Logros` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;
");

            // Crear UsuarioLogros solo si no existe
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `UsuarioLogros` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UsuarioId` int NOT NULL,
    `LogroId` int NOT NULL,
    `FechaObtencion` datetime(6) NOT NULL,
    CONSTRAINT `PK_UsuarioLogros` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UsuarioLogros_Logros_LogroId` FOREIGN KEY (`LogroId`) REFERENCES `Logros` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UsuarioLogros_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;
");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 4, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(138));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 9, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(154), new DateTime(2025, 11, 24, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(156) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 14, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(176));

            // Seed solo si la tabla Logros está vacía (idempotente)
            migrationBuilder.Sql(@"
INSERT INTO `Logros` (`Id`, `CodigoInterno`, `Color`, `Descripcion`, `Icono`, `Nombre`, `Puntos`)
SELECT * FROM (
    SELECT 1, 'PRIMER_PRESTAMO', 'primary', 'Realizar tu primer préstamo', 'fa-book-reader', 'Primeros Pasos', 10 UNION ALL
    SELECT 2, '5_PRESTAMOS', 'info', 'Completar 5 préstamos', 'fa-book-open', 'Lector Constante', 50 UNION ALL
    SELECT 3, '10_PRESTAMOS', 'warning', 'Completar 10 préstamos', 'fa-crown', 'Devorador de Libros', 100 UNION ALL
    SELECT 4, 'PUNTUALIDAD_3', 'success', 'Devolver 3 libros a tiempo consecutivos', 'fa-clock', 'Puntualidad Perfecta', 30
) AS seed
WHERE NOT EXISTS (SELECT 1 FROM `Logros` LIMIT 1);
");

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 30, 15, 27, 9, 962, DateTimeKind.Local).AddTicks(9992));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 9, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(3));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaRegistro", "LetraCurso", "TipoUsuario" },
                values: new object[] { new DateTime(2025, 11, 14, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(92), null, "Alumno" });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaRegistro", "LetraCurso", "TipoUsuario" },
                values: new object[] { new DateTime(2025, 11, 19, 15, 27, 9, 963, DateTimeKind.Local).AddTicks(98), null, "Alumno" });

            // Índices idempotentes: crea solo si no existen
            migrationBuilder.Sql(@"
SET @idx1 := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND table_name = 'UsuarioLogros'
              AND index_name = 'IX_UsuarioLogros_LogroId'
        ),
        'SELECT 0',
        'CREATE INDEX `IX_UsuarioLogros_LogroId` ON `UsuarioLogros` (`LogroId`)'
    )
);
PREPARE stmt FROM @idx1;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.Sql(@"
SET @idx2 := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND table_name = 'UsuarioLogros'
              AND index_name = 'IX_UsuarioLogros_UsuarioId'
        ),
        'SELECT 0',
        'CREATE INDEX `IX_UsuarioLogros_UsuarioId` ON `UsuarioLogros` (`UsuarioId`)'
    )
);
PREPARE stmt FROM @idx2;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropear tablas solo si existen (idempotente)
            migrationBuilder.Sql(@"
SET @drop_ul := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'UsuarioLogros'
        ),
        'DROP TABLE `UsuarioLogros`',
        'SELECT 0'
    )
);
PREPARE stmt FROM @drop_ul;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.Sql(@"
SET @drop_logros := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'Logros'
        ),
        'DROP TABLE `Logros`',
        'SELECT 0'
    )
);
PREPARE stmt FROM @drop_logros;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.Sql(@"
SET @drop_tipo_usuario := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'Usuarios'
              AND column_name = 'TipoUsuario' COLLATE utf8mb4_bin
        ),
        'ALTER TABLE `Usuarios` DROP COLUMN `TipoUsuario`',
        'SELECT 0'
    )
);
PREPARE stmt FROM @drop_tipo_usuario;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.Sql(@"
SET @rename_stmt := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'backup_registros'
              AND column_name = 'TamanoBytes' COLLATE utf8mb4_bin
        ),
        'ALTER TABLE `backup_registros` CHANGE COLUMN `TamanoBytes` `TamañoBytes` bigint NOT NULL',
        'SELECT 0'
    )
);
PREPARE stmt FROM @rename_stmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "LetraCurso",
                keyValue: null,
                column: "LetraCurso",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "LetraCurso",
                table: "Usuarios",
                type: "varchar(1)",
                maxLength: 1,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 3, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(5041));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 8, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(5048), new DateTime(2025, 11, 23, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(5050) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 13, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(5058));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 29, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4682));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 8, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4687));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaRegistro", "LetraCurso" },
                values: new object[] { new DateTime(2025, 11, 13, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4974), "A" });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaRegistro", "LetraCurso" },
                values: new object[] { new DateTime(2025, 11, 18, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4982), "A" });
        }
    }
}
