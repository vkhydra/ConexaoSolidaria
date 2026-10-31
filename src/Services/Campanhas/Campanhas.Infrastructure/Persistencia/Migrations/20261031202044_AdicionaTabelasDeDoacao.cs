using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campanhas.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTabelasDeDoacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chaves_idempotencia",
                columns: table => new
                {
                    chave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    doador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_corpo = table.Column<byte[]>(type: "bytea", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chaves_idempotencia", x => new { x.chave, x.doador_id });
                });

            migrationBuilder.CreateTable(
                name: "doacoes_processadas",
                columns: table => new
                {
                    doacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campanha_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    processada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doacoes_processadas", x => x.doacao_id);
                    table.CheckConstraint("ck_doacoes_processadas_valor", "valor > 0");
                    table.ForeignKey(
                        name: "fk_doacoes_processadas_campanhas_campanha_id",
                        column: x => x.campanha_id,
                        principalTable: "campanhas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chaves_idempotencia_criada_em",
                table: "chaves_idempotencia",
                column: "criada_em");

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_processadas_campanha_id",
                table: "doacoes_processadas",
                column: "campanha_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chaves_idempotencia");

            migrationBuilder.DropTable(
                name: "doacoes_processadas");
        }
    }
}
