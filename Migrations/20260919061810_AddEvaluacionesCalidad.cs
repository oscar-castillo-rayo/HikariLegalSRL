using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluacionesCalidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluacionesCalidad",
                columns: table => new
                {
                    EvaluacionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    Puntuacion = table.Column<byte>(type: "tinyint", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvaluadoPorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesCalidad", x => x.EvaluacionId);
                    table.CheckConstraint("CK_EvaluacionCalidad_Puntuacion", "[Puntuacion] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_EvaluacionesCalidad_AspNetUsers_EvaluadoPorId",
                        column: x => x.EvaluadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EvaluacionesCalidad_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCalidad_EvaluadoPorId",
                table: "EvaluacionesCalidad",
                column: "EvaluadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCalidad_ExpedienteId",
                table: "EvaluacionesCalidad",
                column: "ExpedienteId",
                unique: true);

            migrationBuilder.Sql(@"
CREATE TRIGGER TR_EvaluacionesCalidad_Inmutable
ON EvaluacionesCalidad
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('La evaluación de calidad no puede modificarse ni eliminarse una vez registrada.', 16, 1);
    ROLLBACK TRANSACTION;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_EvaluacionesCalidad_Inmutable;");

            migrationBuilder.DropTable(
                name: "EvaluacionesCalidad");
        }
    }
}
