using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class AddSistemaAlerta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mensaje = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Resuelto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaResuelto = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DetalleTecnico = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 10, 27, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3233));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 1, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3239), new DateTime(2025, 11, 16, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3241) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 6, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3249));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 22, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3055));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 1, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3061));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 6, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3211));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 11, 0, 48, 27, 388, DateTimeKind.Local).AddTicks(3215));

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Fecha",
                table: "Alertas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Resuelto",
                table: "Alertas",
                column: "Resuelto");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 10, 27, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7448));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 11, 1, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7455), new DateTime(2025, 11, 16, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7458) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 6, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7467));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 22, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7026));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 1, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7033));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 6, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7410));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 11, 0, 44, 22, 752, DateTimeKind.Local).AddTicks(7416));
        }
    }
}
