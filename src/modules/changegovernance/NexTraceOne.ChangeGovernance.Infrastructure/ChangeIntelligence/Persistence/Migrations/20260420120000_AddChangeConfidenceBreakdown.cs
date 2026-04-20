using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeConfidenceBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria a tabela principal de breakdowns do Change Confidence Score 2.0.
            // Cada registro representa um snapshot auditável do breakdown para uma release.
            migrationBuilder.CreateTable(
                name: "chg_confidence_breakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregatedScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScoreVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_confidence_breakdowns", x => x.Id);
                });

            // Cria a tabela de sub-scores individuais, relacionados ao breakdown pai.
            // Cada sub-score representa uma dimensão auditável com citação de fonte.
            migrationBuilder.CreateTable(
                name: "chg_confidence_sub_scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BreakdownId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubScoreType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Citations = table.Column<string>(type: "jsonb", nullable: false),
                    SimulatedNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_confidence_sub_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chg_confidence_sub_scores_chg_confidence_breakdowns_Breakdo~",
                        column: x => x.BreakdownId,
                        principalTable: "chg_confidence_breakdowns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chg_confidence_breakdowns_ReleaseId",
                table: "chg_confidence_breakdowns",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_confidence_sub_scores_BreakdownId",
                table: "chg_confidence_sub_scores",
                column: "BreakdownId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chg_confidence_sub_scores");
            migrationBuilder.DropTable(name: "chg_confidence_breakdowns");
        }
    }
}
