using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HikariLegalSRL.Migrations
{
    /// <inheritdoc />
    public partial class AddAbonoAnulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnuladoPorId",
                table: "Abonos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Abonos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Abonos_AnuladoPorId",
                table: "Abonos",
                column: "AnuladoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Abonos_AspNetUsers_AnuladoPorId",
                table: "Abonos",
                column: "AnuladoPorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abonos_AspNetUsers_AnuladoPorId",
                table: "Abonos");

            migrationBuilder.DropIndex(
                name: "IX_Abonos_AnuladoPorId",
                table: "Abonos");

            migrationBuilder.DropColumn(
                name: "AnuladoPorId",
                table: "Abonos");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Abonos");
        }
    }
}
