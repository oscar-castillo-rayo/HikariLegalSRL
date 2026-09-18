using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddEntregableArchivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivoRuta",
                table: "Entregables");

            migrationBuilder.CreateTable(
                name: "EntregableArchivos",
                columns: table => new
                {
                    EntregableArchivoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntregableId = table.Column<int>(type: "int", nullable: false),
                    ArchivoRuta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CargadoPorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregableArchivos", x => x.EntregableArchivoId);
                    table.ForeignKey(
                        name: "FK_EntregableArchivos_AspNetUsers_CargadoPorId",
                        column: x => x.CargadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EntregableArchivos_Entregables_EntregableId",
                        column: x => x.EntregableId,
                        principalTable: "Entregables",
                        principalColumn: "EntregableId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntregableArchivos_CargadoPorId",
                table: "EntregableArchivos",
                column: "CargadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregableArchivos_EntregableId",
                table: "EntregableArchivos",
                column: "EntregableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregableArchivos");

            migrationBuilder.AddColumn<string>(
                name: "ArchivoRuta",
                table: "Entregables",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
