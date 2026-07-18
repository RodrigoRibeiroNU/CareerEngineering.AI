using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerEngineering.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnaliseHistoricoMultiTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Analises_UsuarioId",
                table: "Analises");

            // Renomeia âncoras com nomes semânticos corretos.
            migrationBuilder.RenameColumn(
                name: "VagaText",
                table: "Analises",
                newName: "DescricaoVaga");

            migrationBuilder.RenameColumn(
                name: "CurriculoText",
                table: "Analises",
                newName: "TextoCurriculo");

            migrationBuilder.AddColumn<string>(
                name: "ModeloLLM",
                table: "Analises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "qwen2.5:14b");

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Analises",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "Análise");

            migrationBuilder.CreateTable(
                name: "MensagensHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnaliseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensHistorico_Analises_AnaliseId",
                        column: x => x.AnaliseId,
                        principalTable: "Analises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Migra o antigo Resultado one-shot para a primeira mensagem do assistente.
            migrationBuilder.Sql("""
                INSERT INTO [MensagensHistorico] ([Id], [AnaliseId], [Role], [Conteudo], [DataEnvio])
                SELECT NEWID(), [Id], N'assistant', [Resultado], [DataCriacao]
                FROM [Analises]
                WHERE [Resultado] IS NOT NULL AND LEN([Resultado]) > 0;
                """);

            migrationBuilder.DropColumn(
                name: "Resultado",
                table: "Analises");

            // Título inferido a partir do início da descrição da vaga.
            migrationBuilder.Sql("""
                UPDATE [Analises]
                SET [Titulo] = LEFT(
                    LTRIM(RTRIM(
                        CASE
                            WHEN CHARINDEX(CHAR(10), [DescricaoVaga]) > 0
                                THEN LEFT([DescricaoVaga], CHARINDEX(CHAR(10), [DescricaoVaga]) - 1)
                            ELSE [DescricaoVaga]
                        END
                    )),
                    150
                )
                WHERE [Titulo] = N'Análise' OR [Titulo] = N'';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Analises_UsuarioId_DataCriacao",
                table: "Analises",
                columns: new[] { "UsuarioId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensHistorico_AnaliseId_DataEnvio",
                table: "MensagensHistorico",
                columns: new[] { "AnaliseId", "DataEnvio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensHistorico");

            migrationBuilder.DropIndex(
                name: "IX_Analises_UsuarioId_DataCriacao",
                table: "Analises");

            migrationBuilder.DropColumn(
                name: "ModeloLLM",
                table: "Analises");

            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "Analises");

            migrationBuilder.RenameColumn(
                name: "DescricaoVaga",
                table: "Analises",
                newName: "VagaText");

            migrationBuilder.RenameColumn(
                name: "TextoCurriculo",
                table: "Analises",
                newName: "CurriculoText");

            migrationBuilder.AddColumn<string>(
                name: "Resultado",
                table: "Analises",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Analises_UsuarioId",
                table: "Analises",
                column: "UsuarioId");
        }
    }
}
