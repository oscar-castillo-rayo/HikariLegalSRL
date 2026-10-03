using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddBitacoraAuditoriaInmutableTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_BitacoraAuditoria_Inmutable
ON BitacoraAuditoria
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('La bitácora de auditoría es inalterable: no se permite UPDATE ni DELETE.', 16, 1);
    ROLLBACK TRANSACTION;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_BitacoraAuditoria_Inmutable;");
        }
    }
}
