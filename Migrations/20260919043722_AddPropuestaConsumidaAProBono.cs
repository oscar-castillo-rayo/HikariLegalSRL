using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddPropuestaConsumidaAProBono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropuestaConsumidaId",
                table: "SolicitudesProBono",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_PropuestaConsumidaId",
                table: "SolicitudesProBono",
                column: "PropuestaConsumidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesProBono_Propuestas_PropuestaConsumidaId",
                table: "SolicitudesProBono",
                column: "PropuestaConsumidaId",
                principalTable: "Propuestas",
                principalColumn: "PropuestaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesProBono_Propuestas_PropuestaConsumidaId",
                table: "SolicitudesProBono");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesProBono_PropuestaConsumidaId",
                table: "SolicitudesProBono");

            migrationBuilder.DropColumn(
                name: "PropuestaConsumidaId",
                table: "SolicitudesProBono");
        }
    }
}
