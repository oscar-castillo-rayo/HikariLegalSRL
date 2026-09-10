using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddDireccionesTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER TR_Direcciones_TipoUbicacion
ON Direcciones
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(PaisId) AND NOT UPDATE(DireccionId) RETURN;

    UPDATE d
    SET d.TipoUbicacion = CASE WHEN p.EsPaisBase = 1 THEN 'nacional' ELSE 'extranjero' END
    FROM Direcciones d
    JOIN inserted i ON i.DireccionId = d.DireccionId
    JOIN Paises p ON p.PaisId = d.PaisId;
END;
");
            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER TR_Direcciones_Validar
ON Direcciones
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Paises p ON p.PaisId = i.PaisId
        WHERE p.EsPaisBase = 1 AND i.DistritoId IS NULL
    )
    BEGIN
        RAISERROR('Una direccion nacional debe indicar provincia, canton y distrito.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Paises p ON p.PaisId = i.PaisId
        WHERE p.EsPaisBase = 0 AND i.DistritoId IS NOT NULL
    )
    BEGIN
        RAISERROR('Una direccion extranjera no lleva distrito de Costa Rica y debe escribirse manualmente en SenasExactas.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Direcciones_Validar;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Direcciones_TipoUbicacion;");
        }
    }
}
