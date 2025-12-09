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
            // Fix for MariaDB 10.4: Use CHANGE COLUMN instead of RENAME COLUMN
            migrationBuilder.Sql("ALTER TABLE `backup_registros` CHANGE COLUMN `TamañoBytes` `TamanoBytes` bigint NOT NULL");
            /*
            migrationBuilder.RenameColumn(
                name: "TamañoBytes",
                table: "backup_registros",
                newName: "TamanoBytes");
            */

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

            migrationBuilder.AddColumn<string>(
                name: "TipoUsuario",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Alumno")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Logros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icono = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoInterno = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Puntos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logros", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsuarioLogros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    LogroId = table.Column<int>(type: "int", nullable: false),
                    FechaObtencion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioLogros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioLogros_Logros_LogroId",
                        column: x => x.LogroId,
                        principalTable: "Logros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioLogros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.InsertData(
                table: "Logros",
                columns: new[] { "Id", "CodigoInterno", "Color", "Descripcion", "Icono", "Nombre", "Puntos" },
                values: new object[,]
                {
                    { 1, "PRIMER_PRESTAMO", "primary", "Realizar tu primer préstamo", "fa-book-reader", "Primeros Pasos", 10 },
                    { 2, "5_PRESTAMOS", "info", "Completar 5 préstamos", "fa-book-open", "Lector Constante", 50 },
                    { 3, "10_PRESTAMOS", "warning", "Completar 10 préstamos", "fa-crown", "Devorador de Libros", 100 },
                    { 4, "PUNTUALIDAD_3", "success", "Devolver 3 libros a tiempo consecutivos", "fa-clock", "Puntualidad Perfecta", 30 }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioLogros_LogroId",
                table: "UsuarioLogros",
                column: "LogroId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioLogros_UsuarioId",
                table: "UsuarioLogros",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioLogros");

            migrationBuilder.DropTable(
                name: "Logros");

            migrationBuilder.DropColumn(
                name: "TipoUsuario",
                table: "Usuarios");

            migrationBuilder.RenameColumn(
                name: "TamanoBytes",
                table: "backup_registros",
                newName: "TamañoBytes");

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
