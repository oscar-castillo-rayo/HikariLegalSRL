using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistroHoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosHoras",
                columns: table => new
                {
                    RegistroHorasId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    RondaRevision = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Minutos = table.Column<int>(type: "int", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosHoras", x => x.RegistroHorasId);
                    table.CheckConstraint("CK_RegistroHoras_Minutos", "[Minutos] > 0");
                    table.CheckConstraint("CK_RegistroHoras_Rol", "[Rol] IN ('colaborador', 'revisor')");
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "TareaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_TareaId",
                table: "RegistrosHoras",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_UsuarioId",
                table: "RegistrosHoras",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosHoras");
        }
    }
}
