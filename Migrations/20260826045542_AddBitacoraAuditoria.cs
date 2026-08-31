using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddBitacoraAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitacoraAuditoria",
                columns: table => new
                {
                    BitacoraAuditoriaId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TipoAccion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModuloAfectado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegistroAfectadoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacoraAuditoria", x => x.BitacoraAuditoriaId);
                    table.CheckConstraint("CK_BitacoraAuditoria_TipoAccion", "[TipoAccion] IN ('crear', 'editar', 'eliminar', 'cambiar_estado', 'aprobar', 'rechazar')");
                    table.ForeignKey(
                        name: "FK_BitacoraAuditoria_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitacoraAuditoria_FechaHora",
                table: "BitacoraAuditoria",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_BitacoraAuditoria_UsuarioId",
                table: "BitacoraAuditoria",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitacoraAuditoria");
        }
    }
}
