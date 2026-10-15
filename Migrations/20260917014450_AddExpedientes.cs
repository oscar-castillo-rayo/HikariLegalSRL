using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddExpedientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expedientes",
                columns: table => new
                {
                    ExpedienteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    PropuestaId = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlazoComprometido = table.Column<DateTime>(type: "date", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expedientes", x => x.ExpedienteId);
                    table.CheckConstraint("CK_Expediente_Estado", "[Estado] IN ('abierto', 'cerrado')");
                    table.ForeignKey(
                        name: "FK_Expedientes_AspNetUsers_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Expedientes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId");
                    table.ForeignKey(
                        name: "FK_Expedientes_Propuestas_PropuestaId",
                        column: x => x.PropuestaId,
                        principalTable: "Propuestas",
                        principalColumn: "PropuestaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_ClienteId",
                table: "Expedientes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_PropuestaId",
                table: "Expedientes",
                column: "PropuestaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_ResponsableId",
                table: "Expedientes",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expedientes");
        }
    }
}
