using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ctr_canonical_entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReusePolicy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KnownUsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_canonical_entities", x => x.Id);
                    table.CheckConstraint("CK_ctr_canonical_entities_state", "state IN ('Draft', 'Published', 'Deprecated', 'Retired')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProposedVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Editing"),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AiGenerationPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    LastEditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEditedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_drafts", x => x.Id);
                    table.CheckConstraint("CK_ctr_contract_drafts_status", "status IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    LifecycleState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ImportedFrom = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignatureFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SignatureAlgorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProvenanceOrigin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceOriginalFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceParserUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProvenanceStandardVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProvenanceImportedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProvenanceIsAiGenerated = table.Column<bool>(type: "boolean", nullable: true),
                    ProvenanceAiModelVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_versions", x => x.Id);
                    table.CheckConstraint("CK_ctr_contract_versions_lifecycle_state", "lifecycle_state IN ('Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired')");
                    table.CheckConstraint("CK_ctr_contract_versions_protocol", "protocol IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_spectral_rulesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultExecutionMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnforcementBehavior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicableServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicableProtocols = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_spectral_rulesets", x => x.Id);
                    table.CheckConstraint("CK_ctr_spectral_rulesets_origin", "origin IN ('Platform', 'Organization', 'Team', 'Imported')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_examples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExampleType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_examples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_examples_ctr_contract_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "ctr_contract_drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_artifacts_ctr_contract_versions_ContractVersio~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_diffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChanges = table.Column<string>(type: "text", nullable: false),
                    NonBreakingChanges = table.Column<string>(type: "text", nullable: false),
                    AdditiveChanges = table.Column<string>(type: "text", nullable: false),
                    SuggestedSemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.8m),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_diffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_diffs_ctr_contract_versions_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_evidence_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RuleViolationCount = table.Column<int>(type: "integer", nullable: false),
                    RequiresWorkflowApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresChangeNotification = table.Column<bool>(type: "boolean", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TechnicalSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ImpactedConsumers = table.Column<string[]>(type: "text[]", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_evidence_packs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_evidence_packs_ctr_contract_versions_ContractV~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_rule_violations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SuggestedFix = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_rule_violations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_rule_violations_ctr_contract_versions_Contract~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_scorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CompletenessScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CompatibilityScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    OperationCount = table.Column<int>(type: "integer", nullable: false),
                    SchemaCount = table.Column<int>(type: "integer", nullable: false),
                    HasSecurityDefinitions = table.Column<bool>(type: "boolean", nullable: false),
                    HasExamples = table.Column<bool>(type: "boolean", nullable: false),
                    HasDescriptions = table.Column<bool>(type: "boolean", nullable: false),
                    QualityJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CompletenessJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CompatibilityJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RiskJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_scorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_scorecards_ctr_contract_versions_ContractVersi~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_Domain",
                table: "ctr_canonical_entities",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities",
                column: "IsDeleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_Name",
                table: "ctr_canonical_entities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_OrganizationId",
                table: "ctr_canonical_entities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_State",
                table: "ctr_canonical_entities",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_artifacts_ArtifactType",
                table: "ctr_contract_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_artifacts_ContractVersionId",
                table: "ctr_contract_artifacts",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_diffs_ContractVersionId",
                table: "ctr_contract_diffs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Author",
                table: "ctr_contract_drafts",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts",
                column: "IsDeleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Protocol",
                table: "ctr_contract_drafts",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_ServiceId",
                table: "ctr_contract_drafts",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Status",
                table: "ctr_contract_drafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ApiAssetId",
                table: "ctr_contract_evidence_packs",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ChangeLevel",
                table: "ctr_contract_evidence_packs",
                column: "ChangeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ContractVersionId",
                table: "ctr_contract_evidence_packs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_examples_ContractVersionId",
                table: "ctr_contract_examples",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_examples_DraftId",
                table: "ctr_contract_examples",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_reviews_DraftId",
                table: "ctr_contract_reviews",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_rule_violations_ContractVersionId",
                table: "ctr_contract_rule_violations",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_rule_violations_RulesetId",
                table: "ctr_contract_rule_violations",
                column: "RulesetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_scorecards_ContractVersionId",
                table: "ctr_contract_scorecards",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_scorecards_OverallScore",
                table: "ctr_contract_scorecards",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_ApiAssetId_SemVer",
                table: "ctr_contract_versions",
                columns: new[] { "ApiAssetId", "SemVer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions",
                column: "IsDeleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_LifecycleState",
                table: "ctr_contract_versions",
                column: "LifecycleState");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_Protocol",
                table: "ctr_contract_versions",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_CreatedAt",
                table: "ctr_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_IdempotencyKey",
                table: "ctr_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_ProcessedAt",
                table: "ctr_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets",
                column: "IsActive",
                filter: "\"is_active\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets",
                column: "IsDeleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_Name",
                table: "ctr_spectral_rulesets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_OrganizationId",
                table: "ctr_spectral_rulesets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_Origin",
                table: "ctr_spectral_rulesets",
                column: "Origin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_canonical_entities");

            migrationBuilder.DropTable(
                name: "ctr_contract_artifacts");

            migrationBuilder.DropTable(
                name: "ctr_contract_diffs");

            migrationBuilder.DropTable(
                name: "ctr_contract_evidence_packs");

            migrationBuilder.DropTable(
                name: "ctr_contract_examples");

            migrationBuilder.DropTable(
                name: "ctr_contract_reviews");

            migrationBuilder.DropTable(
                name: "ctr_contract_rule_violations");

            migrationBuilder.DropTable(
                name: "ctr_contract_scorecards");

            migrationBuilder.DropTable(
                name: "ctr_outbox_messages");

            migrationBuilder.DropTable(
                name: "ctr_spectral_rulesets");

            migrationBuilder.DropTable(
                name: "ctr_contract_drafts");

            migrationBuilder.DropTable(
                name: "ctr_contract_versions");
        }
    }
}
