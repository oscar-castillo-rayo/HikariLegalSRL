using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddPropuestas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Propuestas",
                columns: table => new
                {
                    PropuestaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PlazoDias = table.Column<int>(type: "int", nullable: false),
                    ModalidadPago = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DescripcionGeneral = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ElaboradaPorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propuestas", x => x.PropuestaId);
                    table.CheckConstraint("CK_Propuesta_Destinatario", "([ProspectoId] IS NOT NULL AND [ClienteId] IS NULL) OR ([ProspectoId] IS NULL AND [ClienteId] IS NOT NULL)");
                    table.CheckConstraint("CK_Propuesta_Estado", "[Estado] IN ('borrador', 'enviada', 'aceptada', 'rechazada')");
                    table.CheckConstraint("CK_Propuesta_ModalidadPago", "[ModalidadPago] IN ('contado', 'abono', 'pro_bono')");
                    table.CheckConstraint("CK_Propuesta_Moneda", "[Moneda] IN ('colones', 'dolares')");
                    table.CheckConstraint("CK_Propuesta_PlazoDias", "[PlazoDias] > 0");
                    table.ForeignKey(
                        name: "FK_Propuestas_AspNetUsers_ElaboradaPorId",
                        column: x => x.ElaboradaPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Propuestas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId");
                    table.ForeignKey(
                        name: "FK_Propuestas_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "ProspectoId");
                });

            migrationBuilder.CreateTable(
                name: "PropuestaServicios",
                columns: table => new
                {
                    PropuestaServicioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropuestaId = table.Column<int>(type: "int", nullable: false),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    DescripcionServicio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TipoServicio = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropuestaServicios", x => x.PropuestaServicioId);
                    table.CheckConstraint("CK_PropuestaServicio_Precio", "[Precio] >= 0");
                    table.CheckConstraint("CK_PropuestaServicio_TipoServicio", "[TipoServicio] IN ('ofrecido', 'solicitado')");
                    table.ForeignKey(
                        name: "FK_PropuestaServicios_CatalogoServicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "CatalogoServicios",
                        principalColumn: "ServicioId");
                    table.ForeignKey(
                        name: "FK_PropuestaServicios_Propuestas_PropuestaId",
                        column: x => x.PropuestaId,
                        principalTable: "Propuestas",
                        principalColumn: "PropuestaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Propuestas_ClienteId",
                table: "Propuestas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Propuestas_ElaboradaPorId",
                table: "Propuestas",
                column: "ElaboradaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Propuestas_ProspectoId",
                table: "Propuestas",
                column: "ProspectoId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestaServicios_PropuestaId",
                table: "PropuestaServicios",
                column: "PropuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestaServicios_ServicioId",
                table: "PropuestaServicios",
                column: "ServicioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropuestaServicios");

            migrationBuilder.DropTable(
                name: "Propuestas");
        }
    }
}
