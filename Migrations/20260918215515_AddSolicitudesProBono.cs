using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudesProBono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesProBono",
                columns: table => new
                {
                    SolicitudProBonoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    ProspectoId = table.Column<int>(type: "int", nullable: true),
                    SolicitanteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JustificacionEscrita = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ComentarioResolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResueltoPorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesProBono", x => x.SolicitudProBonoId);
                    table.CheckConstraint("CK_ProBono_Beneficiario", "([ClienteId] IS NOT NULL AND [ProspectoId] IS NULL) OR ([ClienteId] IS NULL AND [ProspectoId] IS NOT NULL)");
                    table.CheckConstraint("CK_ProBono_Decision", "[Decision] IN ('pendiente', 'aprobada', 'rechazada')");
                    table.ForeignKey(
                        name: "FK_SolicitudesProBono_AspNetUsers_ResueltoPorId",
                        column: x => x.ResueltoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitudesProBono_AspNetUsers_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitudesProBono_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId");
                    table.ForeignKey(
                        name: "FK_SolicitudesProBono_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "ProspectoId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_ClienteId",
                table: "SolicitudesProBono",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_Decision",
                table: "SolicitudesProBono",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_ProspectoId",
                table: "SolicitudesProBono",
                column: "ProspectoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_ResueltoPorId",
                table: "SolicitudesProBono",
                column: "ResueltoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesProBono_SolicitanteId",
                table: "SolicitudesProBono",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesProBono");
        }
    }
}
