using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddTareas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    TareaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ColaboradorResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "date", nullable: false),
                    HorasEstimadas = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Prioridad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.TareaId);
                    table.CheckConstraint("CK_Tarea_Estado", "[Estado] IN ('pendiente', 'en_proceso', 'lista_revision', 'aprobada', 'devuelta')");
                    table.CheckConstraint("CK_Tarea_HorasEstimadas", "[HorasEstimadas] >= 0");
                    table.CheckConstraint("CK_Tarea_Prioridad", "[Prioridad] IN ('baja', 'media', 'alta')");
                    table.ForeignKey(
                        name: "FK_Tareas_AspNetUsers_ColaboradorResponsableId",
                        column: x => x.ColaboradorResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tareas_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_ColaboradorResponsableId",
                table: "Tareas",
                column: "ColaboradorResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_ExpedienteId",
                table: "Tareas",
                column: "ExpedienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tareas");
        }
    }
}
