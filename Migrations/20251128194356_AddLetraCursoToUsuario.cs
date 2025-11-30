using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class AddLetraCursoToUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LetraCurso",
                table: "Usuarios",
                type: "varchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "A")
                .Annotation("MySql:CharSet", "utf8mb4");

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
                columns: new[] { "FechaRegistro", "LetraCurso" },
                values: new object[] { new DateTime(2025, 11, 13, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3979), "A" });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaRegistro", "LetraCurso" },
                values: new object[] { new DateTime(2025, 11, 18, 16, 43, 56, 77, DateTimeKind.Local).AddTicks(3984), "A" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LetraCurso",
                table: "Usuarios");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 3, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(336));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 8, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(341), new DateTime(2025, 11, 23, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(343) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 13, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(351));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 29, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(172));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 8, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(177));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 13, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(297));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 18, 16, 8, 17, 788, DateTimeKind.Local).AddTicks(301));
        }
    }
}
