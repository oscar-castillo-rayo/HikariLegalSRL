using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisionEntregable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevisionesEntregable",
                columns: table => new
                {
                    RevisionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntregableId = table.Column<int>(type: "int", nullable: false),
                    RevisorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HorasRevision = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ArchivoAdjunto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesEntregable", x => x.RevisionId);
                    table.CheckConstraint("CK_Revision_ObservacionesSiDevuelta", "[Resultado] <> 'devuelta' OR ([Observaciones] IS NOT NULL AND LEN([Observaciones]) > 0)");
                    table.CheckConstraint("CK_RevisionEntregable_HorasRevision", "[HorasRevision] >= 0");
                    table.CheckConstraint("CK_RevisionEntregable_Resultado", "[Resultado] IN ('aprobada', 'devuelta')");
                    table.ForeignKey(
                        name: "FK_RevisionesEntregable_AspNetUsers_RevisorId",
                        column: x => x.RevisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisionesEntregable_Entregables_EntregableId",
                        column: x => x.EntregableId,
                        principalTable: "Entregables",
                        principalColumn: "EntregableId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesEntregable_EntregableId",
                table: "RevisionesEntregable",
                column: "EntregableId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesEntregable_RevisorId",
                table: "RevisionesEntregable",
                column: "RevisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevisionesEntregable");
        }
    }
}
