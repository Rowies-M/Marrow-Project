using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marrow.API.Migrations
{
    /// <inheritdoc />
    public partial class PersistirEstadoInicialPartida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnemigoId",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnemigoX",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnemigoY",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonajeX",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonajeY",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VidaEnemigoActual",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VidaPersonajeActual",
                table: "Partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM "Enemigos"
                        WHERE "Id" = 1 AND "Nombre" = 'Goblin'
                    ) THEN
                        RAISE EXCEPTION 'No existe el Goblin inicial requerido para completar la migracion.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                UPDATE "Partidas" AS partida
                SET "EnemigoId" = 1,
                    "VidaPersonajeActual" = personaje."VidaActual",
                    "VidaEnemigoActual" = enemigo."VidaMaxima",
                    "PersonajeX" = 1,
                    "PersonajeY" = 1,
                    "EnemigoX" = 5,
                    "EnemigoY" = 3
                FROM "Personajes" AS personaje
                CROSS JOIN "Enemigos" AS enemigo
                WHERE partida."PersonajeId" = personaje."Id"
                  AND enemigo."Id" = 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "EnemigoId",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EnemigoX",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EnemigoY",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PersonajeX",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PersonajeY",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VidaEnemigoActual",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VidaPersonajeActual",
                table: "Partidas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partidas_EnemigoId",
                table: "Partidas",
                column: "EnemigoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partidas_Enemigos_EnemigoId",
                table: "Partidas",
                column: "EnemigoId",
                principalTable: "Enemigos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partidas_Enemigos_EnemigoId",
                table: "Partidas");

            migrationBuilder.DropIndex(
                name: "IX_Partidas_EnemigoId",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "EnemigoId",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "EnemigoX",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "EnemigoY",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "PersonajeX",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "PersonajeY",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "VidaEnemigoActual",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "VidaPersonajeActual",
                table: "Partidas");
        }
    }
}
