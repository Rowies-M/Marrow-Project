using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Marrow.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedEnemigosIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Enemigos",
                columns: new[] { "Id", "Ataque", "Defensa", "ExperienciaOtorgada", "Nivel", "Nombre", "VidaMaxima" },
                values: new object[,]
                {
                    { 1, 5, 2, 10, 1, "Goblin", 30 },
                    { 2, 8, 3, 20, 2, "Esqueleto", 45 },
                    { 3, 12, 5, 35, 3, "Orco", 80 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
