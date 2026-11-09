using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marrow.API.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarExperienciaEnemigos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ExperienciaOtorgada",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ExperienciaOtorgada",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 3,
                column: "ExperienciaOtorgada",
                value: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ExperienciaOtorgada",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ExperienciaOtorgada",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Enemigos",
                keyColumn: "Id",
                keyValue: 3,
                column: "ExperienciaOtorgada",
                value: 35);
        }
    }
}
