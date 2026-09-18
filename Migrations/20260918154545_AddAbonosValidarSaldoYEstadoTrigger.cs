using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddAbonosValidarSaldoYEstadoTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER TR_Abonos_ValidarSaldoYEstado
ON Abonos
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Facturas f ON f.FacturaId = i.FacturaId
        WHERE f.Estado = 'anulada'
    )
    BEGIN
        RAISERROR('No se pueden registrar abonos sobre una factura anulada.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Facturas f ON f.FacturaId = i.FacturaId
        WHERE f.MontoTotal < (
            SELECT ISNULL(SUM(a.Monto), 0) FROM Abonos a WHERE a.FacturaId = f.FacturaId
        )
    )
    BEGIN
        RAISERROR('El monto del abono supera el saldo pendiente de la factura.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE f
    SET f.Estado = CASE
        WHEN f.Estado = 'anulada' THEN f.Estado
        WHEN (SELECT ISNULL(SUM(a.Monto),0) FROM Abonos a WHERE a.FacturaId = f.FacturaId) >= f.MontoTotal
             AND f.MontoTotal > 0 THEN 'pagada'
        WHEN (SELECT ISNULL(SUM(a.Monto),0) FROM Abonos a WHERE a.FacturaId = f.FacturaId) > 0 THEN 'pago_parcial'
        ELSE f.Estado
    END
    FROM Facturas f
    JOIN inserted i ON i.FacturaId = f.FacturaId;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Abonos_ValidarSaldoYEstado;");
        }
    }
}
