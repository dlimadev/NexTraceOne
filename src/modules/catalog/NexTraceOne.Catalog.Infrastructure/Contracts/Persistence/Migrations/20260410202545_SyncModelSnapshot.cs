using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_contract_compliance_gates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Rules = table.Column<string>(type: "jsonb", nullable: true),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BlockOnViolation = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_compliance_gates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_compliance_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Violations = table.Column<string>(type: "jsonb", nullable: true),
                    EvidencePackId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_compliance_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_health_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeFrequencyScore = table.Column<int>(type: "integer", nullable: false),
                    ConsumerImpactScore = table.Column<int>(type: "integer", nullable: false),
                    ReviewRecencyScore = table.Column<int>(type: "integer", nullable: false),
                    ExampleCoverageScore = table.Column<int>(type: "integer", nullable: false),
                    PolicyComplianceScore = table.Column<int>(type: "integer", nullable: false),
                    DocumentationScore = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDegraded = table.Column<bool>(type: "boolean", nullable: false),
                    DegradationThreshold = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_health_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    ConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_listings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_negotiations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedByTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByTeamName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Participants = table.Column<string>(type: "jsonb", nullable: false),
                    ParticipantCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    ProposedContractSpec = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_negotiations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_impact_simulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScenarioDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AffectedServices = table.Column<string>(type: "jsonb", nullable: true),
                    BrokenConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    TransitiveCascadeDepth = table.Column<int>(type: "integer", nullable: false),
                    RiskPercent = table.Column<int>(type: "integer", nullable: false),
                    MitigationRecommendations = table.Column<string>(type: "jsonb", nullable: true),
                    SimulatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_impact_simulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_negotiation_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LineReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_negotiation_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_pipeline_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestedStages = table.Column<string>(type: "jsonb", nullable: false),
                    StageResults = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedArtifacts = table.Column<string>(type: "jsonb", nullable: true),
                    TargetLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetFramework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalStages = table.Column<int>(type: "integer", nullable: false),
                    CompletedStages = table.Column<int>(type: "integer", nullable: false),
                    FailedStages = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_pipeline_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_schema_evolution_advices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompatibilityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    FieldsAdded = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsRemoved = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsModified = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsInUseByConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    AffectedConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    AffectedConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StrategyDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Warnings = table.Column<string>(type: "jsonb", nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnalyzedByAgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_schema_evolution_advices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_semantic_diff_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionFromId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContractVersionToId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NaturalLanguageSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AffectedConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    MitigationSuggestions = table.Column<string>(type: "jsonb", nullable: true),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    GeneratedByModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_semantic_diff_results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_IsActive",
                table: "cat_contract_compliance_gates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_Scope",
                table: "cat_contract_compliance_gates",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_Scope_ScopeId_IsActive",
                table: "cat_contract_compliance_gates",
                columns: new[] { "Scope", "ScopeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_ScopeId",
                table: "cat_contract_compliance_gates",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_TenantId",
                table: "cat_contract_compliance_gates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_ChangeId",
                table: "cat_contract_compliance_results",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_ContractVersionId",
                table: "cat_contract_compliance_results",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_EvaluatedAt",
                table: "cat_contract_compliance_results",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_GateId",
                table: "cat_contract_compliance_results",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_Result",
                table: "cat_contract_compliance_results",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_TenantId",
                table: "cat_contract_compliance_results",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_ApiAssetId",
                table: "cat_contract_health_scores",
                column: "ApiAssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_IsDegraded",
                table: "cat_contract_health_scores",
                column: "IsDegraded");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_OverallScore",
                table: "cat_contract_health_scores",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_Category",
                table: "cat_contract_listings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_ContractId",
                table: "cat_contract_listings",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_IsPromoted",
                table: "cat_contract_listings",
                column: "IsPromoted");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_PublishedAt",
                table: "cat_contract_listings",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_Status",
                table: "cat_contract_listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_ContractId",
                table: "cat_contract_negotiations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_CreatedAt",
                table: "cat_contract_negotiations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_ProposedByTeamId",
                table: "cat_contract_negotiations",
                column: "ProposedByTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_Status",
                table: "cat_contract_negotiations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_AuthorId",
                table: "cat_contract_reviews",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_ListingId",
                table: "cat_contract_reviews",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_ReviewedAt",
                table: "cat_contract_reviews",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_RiskPercent",
                table: "cat_impact_simulations",
                column: "RiskPercent");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_Scenario",
                table: "cat_impact_simulations",
                column: "Scenario");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_ServiceName",
                table: "cat_impact_simulations",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_SimulatedAt",
                table: "cat_impact_simulations",
                column: "SimulatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_negotiation_comments_AuthorId",
                table: "cat_negotiation_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_negotiation_comments_NegotiationId",
                table: "cat_negotiation_comments",
                column: "NegotiationId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_ApiAssetId",
                table: "cat_pipeline_executions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_StartedAt",
                table: "cat_pipeline_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_Status",
                table: "cat_pipeline_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_AnalyzedAt",
                table: "cat_schema_evolution_advices",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_ApiAssetId",
                table: "cat_schema_evolution_advices",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_CompatibilityLevel",
                table: "cat_schema_evolution_advices",
                column: "CompatibilityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_Classification",
                table: "cat_semantic_diff_results",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_ContractVersionFromId",
                table: "cat_semantic_diff_results",
                column: "ContractVersionFromId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_ContractVersionToId",
                table: "cat_semantic_diff_results",
                column: "ContractVersionToId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_GeneratedAt",
                table: "cat_semantic_diff_results",
                column: "GeneratedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_contract_compliance_gates");

            migrationBuilder.DropTable(
                name: "cat_contract_compliance_results");

            migrationBuilder.DropTable(
                name: "cat_contract_health_scores");

            migrationBuilder.DropTable(
                name: "cat_contract_listings");

            migrationBuilder.DropTable(
                name: "cat_contract_negotiations");

            migrationBuilder.DropTable(
                name: "cat_contract_reviews");

            migrationBuilder.DropTable(
                name: "cat_impact_simulations");

            migrationBuilder.DropTable(
                name: "cat_negotiation_comments");

            migrationBuilder.DropTable(
                name: "cat_pipeline_executions");

            migrationBuilder.DropTable(
                name: "cat_schema_evolution_advices");

            migrationBuilder.DropTable(
                name: "cat_semantic_diff_results");
        }
    }
}
