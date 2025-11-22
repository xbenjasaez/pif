using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class AddAuditoriaTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Accion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detalle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioEmail = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_UsuarioId",
                table: "Auditorias",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaAgregado",
                value: new DateTime(2025, 10, 26, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8772));

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaAgregado", "FechaPrestamo" },
                values: new object[] { new DateTime(2025, 10, 31, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8778), new DateTime(2025, 11, 15, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8780) });

            migrationBuilder.UpdateData(
                table: "Libros",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaAgregado",
                value: new DateTime(2025, 11, 5, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8787));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 21, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8633));

            migrationBuilder.UpdateData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 10, 31, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8638));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 5, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8750));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2025, 11, 10, 21, 36, 18, 772, DateTimeKind.Local).AddTicks(8754));
        }
    }
}
