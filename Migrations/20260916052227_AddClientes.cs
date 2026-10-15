using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoOrigenId = table.Column<int>(type: "int", nullable: true),
                    NombreEmpresaPersona = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreContacto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CedulaJuridica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DireccionId = table.Column<int>(type: "int", nullable: false),
                    SectorEconomico = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModalidadPago = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.ClienteId);
                    table.CheckConstraint("CK_Cliente_Estado", "[Estado] IN ('activo', 'inactivo')");
                    table.CheckConstraint("CK_Cliente_ModalidadPago", "[ModalidadPago] IN ('contado', 'abono', 'pro_bono')");
                    table.ForeignKey(
                        name: "FK_Clientes_AspNetUsers_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Clientes_Direcciones_DireccionId",
                        column: x => x.DireccionId,
                        principalTable: "Direcciones",
                        principalColumn: "DireccionId");
                    table.ForeignKey(
                        name: "FK_Clientes_Prospectos_ProspectoOrigenId",
                        column: x => x.ProspectoOrigenId,
                        principalTable: "Prospectos",
                        principalColumn: "ProspectoId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_DireccionId",
                table: "Clientes",
                column: "DireccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_ProspectoOrigenId",
                table: "Clientes",
                column: "ProspectoOrigenId",
                unique: true,
                filter: "[ProspectoOrigenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_ResponsableId",
                table: "Clientes",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
