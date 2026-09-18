using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddEntregableTipoFinalTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER TR_Entregables_TipoFinal
ON RevisionesEntregable
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Entregables e ON e.EntregableId = i.EntregableId
        WHERE e.TipoEntregable = 'final'
    )
    BEGIN
        RAISERROR('No se pueden registrar revisiones sobre un entregable ya marcado como final.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE e
    SET e.TipoEntregable = 'final'
    FROM Entregables e
    JOIN inserted i ON i.EntregableId = e.EntregableId
    WHERE i.Resultado = 'aprobada';
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Entregables_TipoFinal;");
        }
    }
}
