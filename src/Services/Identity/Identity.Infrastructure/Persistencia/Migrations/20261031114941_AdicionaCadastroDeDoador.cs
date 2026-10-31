using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCadastroDeDoador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "consentimento_em",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consentimento_versao",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "cpf_cifrado",
                table: "usuarios",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "cpf_hash",
                table: "usuarios",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_cpf_hash",
                table: "usuarios",
                column: "cpf_hash",
                unique: true,
                filter: "cpf_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_usuarios_cpf_hash",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "consentimento_em",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "consentimento_versao",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "cpf_cifrado",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "cpf_hash",
                table: "usuarios");
        }
    }
}
