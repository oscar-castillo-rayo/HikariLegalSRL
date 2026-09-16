using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddActividadesSeguimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActividadesSeguimiento",
                columns: table => new
                {
                    ActividadSeguimientoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: false),
                    TipoActividad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioRegistroId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActividadesSeguimiento", x => x.ActividadSeguimientoId);
                    table.CheckConstraint("CK_ActividadSeguimiento_TipoActividad", "[TipoActividad] IN ('llamada', 'reunion', 'correo', 'nota', 'propuesta', 'otro')");
                    table.ForeignKey(
                        name: "FK_ActividadesSeguimiento_AspNetUsers_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActividadesSeguimiento_AspNetUsers_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActividadesSeguimiento_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "ProspectoId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesSeguimiento_ProspectoId",
                table: "ActividadesSeguimiento",
                column: "ProspectoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesSeguimiento_ResponsableId",
                table: "ActividadesSeguimiento",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesSeguimiento_UsuarioRegistroId",
                table: "ActividadesSeguimiento",
                column: "UsuarioRegistroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActividadesSeguimiento");
        }
    }
}
