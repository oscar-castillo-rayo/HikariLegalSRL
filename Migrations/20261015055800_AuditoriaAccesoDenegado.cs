using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaAccesoDenegado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BitacoraAuditoria_TipoAccion",
                table: "BitacoraAuditoria");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BitacoraAuditoria_TipoAccion",
                table: "BitacoraAuditoria",
                sql: "[TipoAccion] IN ('crear', 'editar', 'eliminar', 'cambiar_estado', 'aprobar', 'rechazar', 'acceso_denegado')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BitacoraAuditoria_TipoAccion",
                table: "BitacoraAuditoria");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BitacoraAuditoria_TipoAccion",
                table: "BitacoraAuditoria",
                sql: "[TipoAccion] IN ('crear', 'editar', 'eliminar', 'cambiar_estado', 'aprobar', 'rechazar')");
        }
    }
}
