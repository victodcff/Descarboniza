using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Descarboniza.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEmissaoCarbono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmissoesCarbono",
                columns: table => new
                {
                    IdEmissao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdMaquina = table.Column<int>(type: "integer", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumoKwh = table.Column<double>(type: "double precision", nullable: false),
                    FatorEmissao = table.Column<double>(type: "double precision", nullable: false),
                    EmissaoKgCo2 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmissoesCarbono", x => x.IdEmissao);
                    table.ForeignKey(
                        name: "FK_EmissoesCarbono_Maquinas_IdMaquina",
                        column: x => x.IdMaquina,
                        principalTable: "Maquinas",
                        principalColumn: "IdMaquina",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmissoesCarbono_IdMaquina",
                table: "EmissoesCarbono",
                column: "IdMaquina");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmissoesCarbono");
        }
    }
}
