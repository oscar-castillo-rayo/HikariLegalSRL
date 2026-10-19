using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class SepararPermisoRecibirTareas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
                SELECT rc.RoleId, rc.ClaimType, 'expedientes.recibir_tareas'
                FROM AspNetRoleClaims rc
                WHERE rc.ClaimType = 'Permiso'
                  AND rc.ClaimValue = 'expedientes.cargar'
                  AND NOT EXISTS (
                      SELECT 1 FROM AspNetRoleClaims x
                      WHERE x.RoleId = rc.RoleId
                        AND x.ClaimType = rc.ClaimType
                        AND x.ClaimValue = 'expedientes.recibir_tareas');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM AspNetRoleClaims
                WHERE ClaimType = 'Permiso' AND ClaimValue = 'expedientes.recibir_tareas';
            ");
        }
    }
}
