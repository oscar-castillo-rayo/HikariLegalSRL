using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogoServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogoServicios",
                columns: table => new
                {
                    ServicioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AreaCategoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PrecioBase = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TipoServicio = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogoServicios", x => x.ServicioId);
                    table.CheckConstraint("CK_CatalogoServicio_Estado", "[Estado] IN ('activo', 'inactivo')");
                    table.CheckConstraint("CK_CatalogoServicio_PrecioBase", "[PrecioBase] >= 0");
                    table.CheckConstraint("CK_CatalogoServicio_TipoServicio", "[TipoServicio] IN ('ofrecido', 'solicitado')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogoServicios");
        }
    }
}
