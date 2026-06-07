using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnomalyNarratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "text", nullable: true),
                    BaselineComparisonSection = table.Column<string>(type: "text", nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "text", nullable: true),
                    CorrelatedChangesSection = table.Column<string>(type: "text", nullable: true),
                    RecommendedActionsSection = table.Column<string>(type: "text", nullable: true),
                    SeverityJustificationSection = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyNarratives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationAuditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    TeamId = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationValidations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ValidatedBy = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    ObservedOutcome = table.Column<string>(type: "text", nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationValidations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    IncidentId = table.Column<string>(type: "text", nullable: true),
                    ChangeId = table.Column<string>(type: "text", nullable: true),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    TargetScope = table.Column<string>(type: "text", nullable: true),
                    TargetEnvironment = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapacityForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    CurrentUtilizationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    GrowthRatePercentPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedDaysToSaturation = table.Column<int>(type: "integer", nullable: true),
                    SaturationRisk = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeCorrelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    MatchType = table.Column<int>(type: "integer", nullable: false),
                    TimeWindowHours = table.Column<int>(type: "integer", nullable: false),
                    CorrelatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ChangeDescription = table.Column<string>(type: "text", nullable: false),
                    ChangeEnvironment = table.Column<string>(type: "text", nullable: false),
                    ChangeOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LegacyAssetType = table.Column<string>(type: "text", nullable: true),
                    LegacyAssetName = table.Column<string>(type: "text", nullable: true),
                    LegacyAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeCorrelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChaosExperiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ExperimentType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TargetPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Steps = table.Column<List<string>>(type: "text[]", nullable: false),
                    SafetyChecks = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExecutionNotes = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChaosExperiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomCharts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ChartType = table.Column<int>(type: "integer", nullable: false),
                    MetricQuery = table.Column<string>(type: "text", nullable: false),
                    TimeRange = table.Column<string>(type: "text", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCharts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriftFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionComment = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentDriftReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEnvironment = table.Column<string>(type: "text", nullable: false),
                    TargetEnvironment = table.Column<string>(type: "text", nullable: false),
                    AnalyzedDimensions = table.Column<string>(type: "text", nullable: false),
                    ServiceVersionDrifts = table.Column<string>(type: "text", nullable: true),
                    ConfigurationDrifts = table.Column<string>(type: "text", nullable: true),
                    ContractVersionDrifts = table.Column<string>(type: "text", nullable: true),
                    DependencyDrifts = table.Column<string>(type: "text", nullable: true),
                    PolicyDrifts = table.Column<string>(type: "text", nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: true),
                    TotalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    CriticalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    OverallSeverity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentDriftReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealingRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCauseDescription = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActionDetails = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    EstimatedImpact = table.Column<string>(type: "text", nullable: true),
                    RelatedRunbookIds = table.Column<string>(type: "text", nullable: true),
                    HistoricalSuccessRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionResult = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    EvidenceTrail = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealingRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentNarratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "text", nullable: true),
                    TimelineSection = table.Column<string>(type: "text", nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "text", nullable: true),
                    MitigationSection = table.Column<string>(type: "text", nullable: true),
                    RelatedChangesSection = table.Column<string>(type: "text", nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNarratives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentPredictionPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatternName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PatternType = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    ServiceName = table.Column<string>(type: "text", nullable: true),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    TriggerConditions = table.Column<string>(type: "text", nullable: false),
                    PreventionRecommendations = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidationComment = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPredictionPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalRef = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    OwnerTeam = table.Column<string>(type: "text", nullable: false),
                    ImpactedDomain = table.Column<string>(type: "text", nullable: true),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasCorrelation = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationConfidence = table.Column<int>(type: "integer", nullable: false),
                    MitigationStatus = table.Column<int>(type: "integer", nullable: false),
                    CorrelationAnalysis = table.Column<string>(type: "text", nullable: true),
                    EvidenceTelemetrySummary = table.Column<string>(type: "text", nullable: true),
                    EvidenceBusinessImpact = table.Column<string>(type: "text", nullable: true),
                    EvidenceAnalysis = table.Column<string>(type: "text", nullable: true),
                    EvidenceTemporalContext = table.Column<string>(type: "text", nullable: true),
                    MitigationNarrative = table.Column<string>(type: "text", nullable: true),
                    HasEscalationPath = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationPath = table.Column<string>(type: "text", nullable: true),
                    TimelineJson = table.Column<string>(type: "text", nullable: true),
                    LinkedServicesJson = table.Column<string>(type: "text", nullable: true),
                    CorrelatedChangesJson = table.Column<string>(type: "text", nullable: true),
                    CorrelatedServicesJson = table.Column<string>(type: "text", nullable: true),
                    CorrelatedDependenciesJson = table.Column<string>(type: "text", nullable: true),
                    ImpactedContractsJson = table.Column<string>(type: "text", nullable: true),
                    EvidenceObservationsJson = table.Column<string>(type: "text", nullable: true),
                    RelatedContractsJson = table.Column<string>(type: "text", nullable: true),
                    RunbookLinksJson = table.Column<string>(type: "text", nullable: true),
                    MitigationActionsJson = table.Column<string>(type: "text", nullable: true),
                    MitigationRecommendationsJson = table.Column<string>(type: "text", nullable: true),
                    MitigationRecommendedRunbooksJson = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MitigationValidations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ObservedOutcome = table.Column<string>(type: "text", nullable: true),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChecksJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitigationValidations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MitigationWorkflowActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    PerformedBy = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitigationWorkflowActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MitigationWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedRunbookId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUser = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedOutcome = table.Column<int>(type: "integer", nullable: true),
                    CompletedNotes = table.Column<string>(type: "text", nullable: true),
                    CompletedBy = table.Column<string>(type: "text", nullable: true),
                    StepsJson = table.Column<string>(type: "text", nullable: true),
                    DecisionsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitigationWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservabilityProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    HasTracing = table.Column<bool>(type: "boolean", nullable: false),
                    HasMetrics = table.Column<bool>(type: "boolean", nullable: false),
                    HasLogging = table.Column<bool>(type: "boolean", nullable: false),
                    HasAlerting = table.Column<bool>(type: "boolean", nullable: false),
                    HasDashboard = table.Column<bool>(type: "boolean", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    LastAssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservabilityProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_carbon_score_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CpuHours = table.Column<double>(type: "double precision", nullable: false),
                    MemoryGbHours = table.Column<double>(type: "double precision", nullable: false),
                    NetworkGb = table.Column<double>(type: "double precision", nullable: false),
                    CarbonGrams = table.Column<double>(type: "double precision", nullable: false),
                    IntensityFactor = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_carbon_score_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_waste_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estimated_monthly_savings = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_waste_signals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalPlaybooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Steps = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LinkedServiceIds = table.Column<string>(type: "text", nullable: true),
                    LinkedRunbookIds = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalPlaybooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_attributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    CostPerRequest = table.Column<decimal>(type: "numeric", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_budget_forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ForecastPeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    ConfidencePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsOverBudgetProjected = table.Column<bool>(type: "boolean", nullable: false),
                    ForecastNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_budget_forecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_efficiency_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceCost = table.Column<decimal>(type: "numeric", nullable: false),
                    MedianPeerCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RecommendationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_efficiency_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_trends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AverageDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PeakDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    PercentageChange = table.Column<decimal>(type: "numeric", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_trends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_incident_outbox_messages",
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
                    table.PrimaryKey("PK_ops_incident_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_service_cost_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DomainName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TagsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_service_cost_allocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_service_cost_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentMonthCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_service_cost_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_anomaly_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedValue = table.Column<double>(type: "double precision", nullable: false),
                    DeviationPercent = table.Column<double>(type: "double precision", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelatedReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_anomaly_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_dependency_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CallCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_dependency_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_investigation_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TitleMessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvestigationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeWindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnomalySnapshotIds = table.Column<string>(type: "jsonb", nullable: false),
                    ReleaseCorrelationIds = table.Column<string>(type: "jsonb", nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AffectedServiceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AiSummaryJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_investigation_contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_observed_topology",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommunicationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCallCount = table.Column<long>(type: "bigint", nullable: false),
                    IsShadowDependency = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_observed_topology", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_release_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    ImpactScore = table.Column<double>(type: "double precision", nullable: false),
                    ImpactClassification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_release_correlations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_service_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    RequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    RequestsPerHour = table.Column<double>(type: "double precision", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyMaxMs = table.Column<double>(type: "double precision", nullable: false),
                    CpuAvgPercent = table.Column<double>(type: "double precision", nullable: true),
                    MemoryAvgMb = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_service_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_telemetry_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BackendType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessUri = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_telemetry_references", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookName = table.Column<string>(type: "text", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutedByUserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StepResults = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostIncidentReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhase = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    RootCauseAnalysis = table.Column<string>(type: "text", nullable: true),
                    PreventiveActionsJson = table.Column<string>(type: "text", nullable: true),
                    TimelineNarrative = table.Column<string>(type: "text", nullable: true),
                    ResponsibleTeam = table.Column<string>(type: "text", nullable: false),
                    Facilitator = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostIncidentReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfilingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    FrameType = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalCpuSamples = table.Column<long>(type: "bigint", nullable: false),
                    PeakMemoryMb = table.Column<decimal>(type: "numeric", nullable: false),
                    TopFramesJson = table.Column<string>(type: "text", nullable: true),
                    RawDataUri = table.Column<string>(type: "text", nullable: true),
                    RawDataHash = table.Column<string>(type: "text", nullable: true),
                    ReleaseVersion = table.Column<string>(type: "text", nullable: true),
                    CommitSha = table.Column<string>(type: "text", nullable: true),
                    HasAnomalies = table.Column<bool>(type: "boolean", nullable: false),
                    PeakThreadCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReliabilitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RuntimeHealthScore = table.Column<decimal>(type: "numeric", nullable: false),
                    IncidentImpactScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OpenIncidentCount = table.Column<int>(type: "integer", nullable: false),
                    RuntimeHealthStatus = table.Column<string>(type: "text", nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReliabilitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResilienceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChaosExperimentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ExperimentType = table.Column<string>(type: "text", nullable: false),
                    ResilienceScore = table.Column<int>(type: "integer", nullable: false),
                    TheoreticalBlastRadius = table.Column<string>(type: "text", nullable: true),
                    ActualBlastRadius = table.Column<string>(type: "text", nullable: true),
                    BlastRadiusDeviation = table.Column<decimal>(type: "numeric", nullable: true),
                    TelemetryObservations = table.Column<string>(type: "text", nullable: true),
                    LatencyImpactMs = table.Column<decimal>(type: "numeric", nullable: true),
                    ErrorRateImpact = table.Column<decimal>(type: "numeric", nullable: true),
                    RecoveryTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    Strengths = table.Column<string>(type: "text", nullable: true),
                    Weaknesses = table.Column<string>(type: "text", nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResilienceReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    LinkedService = table.Column<string>(type: "text", nullable: true),
                    LinkedIncidentType = table.Column<string>(type: "text", nullable: true),
                    StepsJson = table.Column<string>(type: "text", nullable: true),
                    PrerequisitesJson = table.Column<string>(type: "text", nullable: true),
                    PostNotes = table.Column<string>(type: "text", nullable: true),
                    MaintainedBy = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runbooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeBaselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ExpectedAvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedP99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedRequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    EstablishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeBaselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    P99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuUsagePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryUsageMb = table.Column<decimal>(type: "numeric", nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    HealthStatus = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceFailurePredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    FailureProbabilityPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    PredictionHorizon = table.Column<string>(type: "text", nullable: false),
                    CausalFactors = table.Column<string[]>(type: "text[]", nullable: false),
                    RecommendedAction = table.Column<string>(type: "text", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFailurePredictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SloDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    WindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SloDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SloObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    ObservedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    SloTarget = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SloObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BurnRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    Window = table.Column<int>(type: "integer", nullable: false),
                    BurnRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ObservedErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ToleratedErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    SloDefinitionId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BurnRateSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BurnRateSnapshots_SloDefinitions_SloDefinitionId1",
                        column: x => x.SloDefinitionId1,
                        principalTable: "SloDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErrorBudgetSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    TotalBudgetMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedBudgetMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingBudgetMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    SloDefinitionId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorBudgetSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorBudgetSnapshots_SloDefinitions_SloDefinitionId1",
                        column: x => x.SloDefinitionId1,
                        principalTable: "SloDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ContractualTargetPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasPenaltyClauses = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyNotes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    SloDefinitionId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaDefinitions_SloDefinitions_SloDefinitionId1",
                        column: x => x.SloDefinitionId1,
                        principalTable: "SloDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BurnRateSnapshots_SloDefinitionId1",
                table: "BurnRateSnapshots",
                column: "SloDefinitionId1");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorBudgetSnapshots_SloDefinitionId1",
                table: "ErrorBudgetSnapshots",
                column: "SloDefinitionId1");

            migrationBuilder.CreateIndex(
                name: "IX_oi_carbon_score_records_TenantId_Date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_carbon_score_records_TenantId_ServiceId_Date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "ServiceId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oi_waste_signals_service",
                table: "oi_waste_signals",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "ix_oi_waste_signals_team_ack",
                table: "oi_waste_signals",
                columns: new[] { "team_name", "is_acknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ApiAssetId_Environment_PeriodStart_Pe~",
                table: "ops_cost_attributions",
                columns: new[] { "ApiAssetId", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ServiceName",
                table: "ops_cost_attributions",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ComputedAt",
                table: "ops_cost_budget_forecasts",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ServiceId_Environment",
                table: "ops_cost_budget_forecasts",
                columns: new[] { "ServiceId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_IsAcknowledged",
                table: "ops_cost_efficiency_recommendations",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_ServiceId",
                table: "ops_cost_efficiency_recommendations",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Source_Period",
                table: "ops_cost_import_batches",
                columns: new[] { "Source", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Status",
                table: "ops_cost_import_batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_BatchId",
                table: "ops_cost_records",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Domain",
                table: "ops_cost_records",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Period",
                table: "ops_cost_records",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ReleaseId",
                table: "ops_cost_records",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ServiceId_Period",
                table: "ops_cost_records",
                columns: new[] { "ServiceId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Team",
                table: "ops_cost_records",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_Period",
                table: "ops_cost_snapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_ServiceName_Environment_CapturedAt",
                table: "ops_cost_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_ServiceName_Environment_PeriodStart_PeriodE~",
                table: "ops_cost_trends",
                columns: new[] { "ServiceName", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_TrendDirection",
                table: "ops_cost_trends",
                column: "TrendDirection");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incident_outbox_messages_CreatedAt",
                table: "ops_incident_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incident_outbox_messages_IdempotencyKey",
                table: "ops_incident_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_incident_outbox_messages_ProcessedAt",
                table: "ops_incident_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_service_cost_allocations_TenantId_PeriodStart_PeriodEnd",
                table: "ops_service_cost_allocations",
                columns: new[] { "TenantId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_service_cost_allocations_TenantId_ServiceName_PeriodSta~",
                table: "ops_service_cost_allocations",
                columns: new[] { "TenantId", "ServiceName", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_service_cost_profiles_ServiceName_Environment",
                table: "ops_service_cost_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_ServiceId_Environment_DetectedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "ServiceId", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_Severity_ResolvedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "Severity", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_dependency_metrics_SourceServiceId_TargetServiceId_E~",
                table: "ops_ts_dependency_metrics",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_PrimaryServiceId_Environment_~",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "PrimaryServiceId", "Environment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_Status_CreatedAt",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_Environment_IsShadowDependency",
                table: "ops_ts_observed_topology",
                columns: new[] { "Environment", "IsShadowDependency" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_SourceServiceId_TargetServiceId_En~",
                table: "ops_ts_observed_topology",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "CommunicationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ReleaseId",
                table: "ops_ts_release_correlations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ServiceId_Environment_DeployedAt",
                table: "ops_ts_release_correlations",
                columns: new[] { "ServiceId", "Environment", "DeployedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceId_Environment_AggregationLev~",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceName_Environment_IntervalStart",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceName", "Environment", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_CorrelationId",
                table: "ops_ts_telemetry_references",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_ServiceId_SignalType_OriginalTi~",
                table: "ops_ts_telemetry_references",
                columns: new[] { "ServiceId", "SignalType", "OriginalTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaDefinitions_SloDefinitionId1",
                table: "SlaDefinitions",
                column: "SloDefinitionId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnomalyNarratives");

            migrationBuilder.DropTable(
                name: "AutomationAuditRecords");

            migrationBuilder.DropTable(
                name: "AutomationValidations");

            migrationBuilder.DropTable(
                name: "AutomationWorkflows");

            migrationBuilder.DropTable(
                name: "BurnRateSnapshots");

            migrationBuilder.DropTable(
                name: "CapacityForecasts");

            migrationBuilder.DropTable(
                name: "ChangeCorrelations");

            migrationBuilder.DropTable(
                name: "ChaosExperiments");

            migrationBuilder.DropTable(
                name: "CustomCharts");

            migrationBuilder.DropTable(
                name: "DriftFindings");

            migrationBuilder.DropTable(
                name: "EnvironmentDriftReports");

            migrationBuilder.DropTable(
                name: "ErrorBudgetSnapshots");

            migrationBuilder.DropTable(
                name: "HealingRecommendations");

            migrationBuilder.DropTable(
                name: "IncidentNarratives");

            migrationBuilder.DropTable(
                name: "IncidentPredictionPatterns");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "MitigationValidations");

            migrationBuilder.DropTable(
                name: "MitigationWorkflowActions");

            migrationBuilder.DropTable(
                name: "MitigationWorkflows");

            migrationBuilder.DropTable(
                name: "ObservabilityProfiles");

            migrationBuilder.DropTable(
                name: "oi_carbon_score_records");

            migrationBuilder.DropTable(
                name: "oi_waste_signals");

            migrationBuilder.DropTable(
                name: "OperationalPlaybooks");

            migrationBuilder.DropTable(
                name: "ops_cost_attributions");

            migrationBuilder.DropTable(
                name: "ops_cost_budget_forecasts");

            migrationBuilder.DropTable(
                name: "ops_cost_efficiency_recommendations");

            migrationBuilder.DropTable(
                name: "ops_cost_import_batches");

            migrationBuilder.DropTable(
                name: "ops_cost_records");

            migrationBuilder.DropTable(
                name: "ops_cost_snapshots");

            migrationBuilder.DropTable(
                name: "ops_cost_trends");

            migrationBuilder.DropTable(
                name: "ops_incident_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_service_cost_allocations");

            migrationBuilder.DropTable(
                name: "ops_service_cost_profiles");

            migrationBuilder.DropTable(
                name: "ops_ts_anomaly_snapshots");

            migrationBuilder.DropTable(
                name: "ops_ts_dependency_metrics");

            migrationBuilder.DropTable(
                name: "ops_ts_investigation_contexts");

            migrationBuilder.DropTable(
                name: "ops_ts_observed_topology");

            migrationBuilder.DropTable(
                name: "ops_ts_release_correlations");

            migrationBuilder.DropTable(
                name: "ops_ts_service_metrics");

            migrationBuilder.DropTable(
                name: "ops_ts_telemetry_references");

            migrationBuilder.DropTable(
                name: "PlaybookExecutions");

            migrationBuilder.DropTable(
                name: "PostIncidentReviews");

            migrationBuilder.DropTable(
                name: "ProfilingSessions");

            migrationBuilder.DropTable(
                name: "ReliabilitySnapshots");

            migrationBuilder.DropTable(
                name: "ResilienceReports");

            migrationBuilder.DropTable(
                name: "Runbooks");

            migrationBuilder.DropTable(
                name: "RuntimeBaselines");

            migrationBuilder.DropTable(
                name: "RuntimeSnapshots");

            migrationBuilder.DropTable(
                name: "ServiceFailurePredictions");

            migrationBuilder.DropTable(
                name: "SlaDefinitions");

            migrationBuilder.DropTable(
                name: "SloObservations");

            migrationBuilder.DropTable(
                name: "SloDefinitions");
        }
    }
}
