using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectoAndDirecction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Direcciones_Paises_PaisId",
                table: "Direcciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Prospectos_AspNetUsers_UsuarioCreadorId",
                table: "Prospectos");

            migrationBuilder.DropForeignKey(
                name: "FK_Prospectos_Direcciones_DireccionId",
                table: "Prospectos");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Prospectos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "Prospectos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreEmpresaPersona",
                table: "Prospectos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NombreContacto",
                table: "Prospectos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Prospectos",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Prospectos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CedulaJuridica",
                table: "Prospectos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoUbicacion",
                table: "Direcciones",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SenasExactas",
                table: "Direcciones",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prospectos_Correo",
                table: "Prospectos",
                column: "Correo",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Prospecto_Calificacion",
                table: "Prospectos",
                sql: "[Calificacion] IS NULL OR ([Calificacion] BETWEEN 1 AND 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Prospecto_Estado",
                table: "Prospectos",
                sql: "[Estado] IN ('activo', 'convertido', 'descartado')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Direccion_TipoUbicacion",
                table: "Direcciones",
                sql: "[TipoUbicacion] IN ('nacional', 'extranjero')");

            migrationBuilder.AddForeignKey(
                name: "FK_Direcciones_Paises_PaisId",
                table: "Direcciones",
                column: "PaisId",
                principalTable: "Paises",
                principalColumn: "PaisId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prospectos_AspNetUsers_UsuarioCreadorId",
                table: "Prospectos",
                column: "UsuarioCreadorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prospectos_Direcciones_DireccionId",
                table: "Prospectos",
                column: "DireccionId",
                principalTable: "Direcciones",
                principalColumn: "DireccionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Direcciones_Paises_PaisId",
                table: "Direcciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Prospectos_AspNetUsers_UsuarioCreadorId",
                table: "Prospectos");

            migrationBuilder.DropForeignKey(
                name: "FK_Prospectos_Direcciones_DireccionId",
                table: "Prospectos");

            migrationBuilder.DropIndex(
                name: "IX_Prospectos_Correo",
                table: "Prospectos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Prospecto_Calificacion",
                table: "Prospectos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Prospecto_Estado",
                table: "Prospectos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Direccion_TipoUbicacion",
                table: "Direcciones");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreEmpresaPersona",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "NombreContacto",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "CedulaJuridica",
                table: "Prospectos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoUbicacion",
                table: "Direcciones",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "SenasExactas",
                table: "Direcciones",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Direcciones_Paises_PaisId",
                table: "Direcciones",
                column: "PaisId",
                principalTable: "Paises",
                principalColumn: "PaisId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prospectos_AspNetUsers_UsuarioCreadorId",
                table: "Prospectos",
                column: "UsuarioCreadorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prospectos_Direcciones_DireccionId",
                table: "Prospectos",
                column: "DireccionId",
                principalTable: "Direcciones",
                principalColumn: "DireccionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
