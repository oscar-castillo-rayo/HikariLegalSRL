using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddEntregables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entregables",
                columns: table => new
                {
                    EntregableId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    ArchivoRuta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RondaRevision = table.Column<int>(type: "int", nullable: false),
                    HorasReales = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    TipoEntregable = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    CargadoPorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregables", x => x.EntregableId);
                    table.CheckConstraint("CK_Entregable_HorasReales", "[HorasReales] >= 0");
                    table.CheckConstraint("CK_Entregable_TipoEntregable", "[TipoEntregable] IN ('preliminar', 'final')");
                    table.ForeignKey(
                        name: "FK_Entregables_AspNetUsers_CargadoPorId",
                        column: x => x.CargadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Entregables_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "TareaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_CargadoPorId",
                table: "Entregables",
                column: "CargadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_TareaId",
                table: "Entregables",
                column: "TareaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entregables");
        }
    }
}
