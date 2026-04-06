using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations;

/// <summary>
/// Adiciona a tabela ctr_consumer_expectations para suporte a Consumer-Driven Contract Testing (CDCT).
/// Permite que consumidores registem expectativas sobre contratos publicados,
/// viabilizando verificação automática de compatibilidade provider/consumer.
/// </summary>
public partial class AddConsumerExpectations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ctr_consumer_expectations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                ConsumerServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ConsumerDomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExpectedSubsetJson = table.Column<string>(type: "text", nullable: false),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ctr_consumer_expectations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ctr_consumer_expectations_ApiAssetId",
            table: "ctr_consumer_expectations",
            column: "ApiAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_ctr_consumer_expectations_ApiAssetId_ConsumerServiceName",
            table: "ctr_consumer_expectations",
            columns: new[] { "ApiAssetId", "ConsumerServiceName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ctr_consumer_expectations_IsActive",
            table: "ctr_consumer_expectations",
            column: "IsActive",
            filter: "\"IsActive\" = true");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ctr_consumer_expectations");
    }
}
