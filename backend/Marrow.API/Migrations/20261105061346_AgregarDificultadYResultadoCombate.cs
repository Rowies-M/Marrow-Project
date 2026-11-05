using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marrow.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDificultadYResultadoCombate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DificultadDesbloqueada",
                table: "Personajes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Dificultad",
                table: "Partidas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResultadoCombate",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Personajes"
                SET "VidaActual" = 80,
                    "VidaMaxima" = 80,
                    "Ataque" = 15;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DificultadDesbloqueada",
                table: "Personajes");

            migrationBuilder.DropColumn(
                name: "Dificultad",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "ResultadoCombate",
                table: "Partidas");
        }
    }
}
