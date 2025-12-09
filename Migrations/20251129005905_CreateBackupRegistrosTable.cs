using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class CreateBackupRegistrosTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.AddColumn<string>(
                name: "Genero",
                table: "Usuarios",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            */

            /*
            migrationBuilder.CreateTable(
                name: "backup_registros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NombreArchivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaCompleta = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TamañoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Exitoso = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_registros", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 13, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4974));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 18, 21, 59, 4, 936, DateTimeKind.Local).AddTicks(4982));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backup_registros");

            /*
            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Usuarios");
            */

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 3, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(4006));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 8, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(4012), new DateTime(2025, 11, 23, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(4014) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 13, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(4022));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 29, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3705));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 8, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3710));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 13, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3979));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 18, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3984));
        }
    }
}
