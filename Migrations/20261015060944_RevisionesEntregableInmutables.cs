using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class RevisionesEntregableInmutables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER TR_RevisionesEntregable_Inmutable
ON RevisionesEntregable
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM inserted i
            JOIN deleted d ON d.RevisionId = i.RevisionId
            WHERE i.EntregableId <> d.EntregableId
               OR i.RevisorId <> d.RevisorId
               OR i.Resultado <> d.Resultado
               OR i.HorasRevision <> d.HorasRevision
               OR i.FechaRevision <> d.FechaRevision
               OR ISNULL(i.Observaciones, '') <> ISNULL(d.Observaciones, '')
               OR (d.ArchivoAdjunto IS NOT NULL AND ISNULL(i.ArchivoAdjunto, '') <> d.ArchivoAdjunto)
        )
        BEGIN
            RAISERROR('El historial de revisiones es inalterable: una revisión registrada no puede modificarse.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
    ELSE IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN Entregables en ON en.EntregableId = d.EntregableId
        JOIN Entregables fin ON fin.TareaId = en.TareaId AND fin.TipoEntregable = 'final'
    )
    BEGIN
        RAISERROR('El historial de revisiones de una tarea aprobada es inalterable: no se permite eliminarlo.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_RevisionesEntregable_Inmutable;");
        }
    }
}
