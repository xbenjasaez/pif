using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaVirtualWeb.Migrations
{
    public partial class Baseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La base actual ya contiene este esquema (aplicado manualmente en MySQL).
            // Esta migración sirve solo como punto de partida para futuras migraciones.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se revertirá nada en el baseline.
        }
    }
}

