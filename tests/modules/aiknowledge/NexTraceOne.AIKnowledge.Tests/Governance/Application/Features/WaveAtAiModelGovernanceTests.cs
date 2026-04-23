using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiGovernanceComplianceReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiModelQualityReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelDriftReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.IngestModelPredictionSample;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using System.Linq;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para Wave AT — AI Model Quality &amp; Drift Governance.
/// Cobre AT.1 IngestModelPredictionSample + GetModelDriftReport,
/// AT.2 GetAiModelQualityReport, AT.3 GetAiGovernanceComplianceReport.
/// </summary>
public sealed class WaveAtAiModelGovernanceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-at-001";
    private static readonly Guid ModelA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid ModelB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IConfigurationResolutionService CreateConfigService(
        string? inputDriftWarning = null,
        string? outputDriftWarning = null,
        string? minSamples = null,
        string? lowConf = null,
        string? latencyBudget = null,
        string? reviewDays = null,
        string? overrunThreshold = null,
        string? auditLookback = null)
    {
        var svc = Substitute.For<IConfigurationResolutionService>();
        // Catch-all returning null — individual keys return defaults
        svc.ResolveEffectiveValueAsync(
                Arg.Any<string>(),
                Arg.Any<NexTraceOne.Configuration.Domain.Enums.ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(null));
        return svc;
    }

    private static void SetupConfig(IConfigurationResolutionService svc, string key, string? value)
    {
        EffectiveConfigurationDto? result = value is null
            ? null
            : new EffectiveConfigurationDto(key, value, "System", null, false, false, key, "string", false, 1);
        svc.ResolveEffectiveValueAsync(key, Arg.Any<NexTraceOne.Configuration.Domain.Enums.ConfigurationScope>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(result);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AT.1 — IngestModelPredictionSample
    // ════════════════════════════════════════════════════════════════════════

    private static IngestModelPredictionSample.Handler CreateIngestHandler(
        IModelPredictionRepository? repo = null)
    {
        repo ??= Substitute.For<IModelPredictionRepository>();
        return new IngestModelPredictionSample.Handler(repo, CreateClock());
    }

    private static IngestModelPredictionSample.Command ValidIngestCommand(
        Guid? modelId = null,
        string modelName = "gpt-4o",
        double confidenceScore = 0.92) =>
        new(TenantId, modelId ?? ModelA, modelName, "svc-1",
            FixedNow.AddMinutes(-5), "{\"feat1\":{\"mean\":0.5}}", "ClassA",
            confidenceScore, 120, null, false);

    [Fact]
    public async Task IngestModelPredictionSample_ValidCommand_ReturnsGuidAndCallsRepository()
    {
        var repo = Substitute.For<IModelPredictionRepository>();
        var handler = CreateIngestHandler(repo);

        var result = await handler.Handle(ValidIngestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await repo.Received(1).AddAsync(Arg.Any<ModelPredictionSample>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestModelPredictionSample_PredictedAtDefault_UsesClockNow()
    {
        ModelPredictionSample? captured = null;
        var repo = Substitute.For<IModelPredictionRepository>();
        repo.When(r => r.AddAsync(Arg.Any<ModelPredictionSample>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<ModelPredictionSample>());
        var handler = CreateIngestHandler(repo);

        var cmd = new IngestModelPredictionSample.Command(
            TenantId, ModelA, "gpt-4o", "svc-1", default, null, null, null, null, null, false);
        await handler.Handle(cmd, CancellationToken.None);

        captured!.PredictedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task IngestModelPredictionSample_WithActualClass_SetsFeedbackLoop()
    {
        ModelPredictionSample? captured = null;
        var repo = Substitute.For<IModelPredictionRepository>();
        repo.When(r => r.AddAsync(Arg.Any<ModelPredictionSample>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<ModelPredictionSample>());
        var handler = CreateIngestHandler(repo);

        var cmd = new IngestModelPredictionSample.Command(
            TenantId, ModelA, "model", "svc-1", FixedNow, null, "ClassA", 0.9, 100, "ClassA", false);
        await handler.Handle(cmd, CancellationToken.None);

        captured!.ActualClass.Should().Be("ClassA");
    }

    [Fact]
    public async Task IngestModelPredictionSample_Fallback_SetsFallbackFlag()
    {
        ModelPredictionSample? captured = null;
        var repo = Substitute.For<IModelPredictionRepository>();
        repo.When(r => r.AddAsync(Arg.Any<ModelPredictionSample>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<ModelPredictionSample>());
        var handler = CreateIngestHandler(repo);

        var cmd = ValidIngestCommand() with { IsFallback = true };
        await handler.Handle(cmd, CancellationToken.None);

        captured!.IsFallback.Should().BeTrue();
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("ModelName")]
    [InlineData("ServiceId")]
    public async Task IngestModelPredictionSample_MissingRequiredField_ValidationFails(string fieldName)
    {
        var validator = new IngestModelPredictionSample.Validator();
        var cmd = fieldName switch
        {
            "TenantId" => ValidIngestCommand() with { TenantId = "" },
            "ModelName" => ValidIngestCommand() with { ModelName = "" },
            "ServiceId" => ValidIngestCommand() with { ServiceId = "" },
            _ => throw new ArgumentException($"Unknown field: {fieldName}")
        };
        var validationResult = await validator.ValidateAsync(cmd);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IngestModelPredictionSample_ConfidenceScoreOutOfRange_ValidationFails()
    {
        var validator = new IngestModelPredictionSample.Validator();
        var cmd = ValidIngestCommand(confidenceScore: 1.5);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AT.1 — GetModelDriftReport
    // ════════════════════════════════════════════════════════════════════════

    private GetModelDriftReport.Handler CreateDriftHandler(
        IModelDriftReader? reader = null)
    {
        reader ??= new NullModelDriftReader();
        return new GetModelDriftReport.Handler(
            reader, CreateConfigService(), CreateClock());
    }

    [Fact]
    public async Task GetModelDriftReport_NoData_ReturnsEmptyReport()
    {
        var handler = CreateDriftHandler();

        var result = await handler.Handle(
            new GetModelDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByModel.Should().BeEmpty();
        result.Value.DriftAlerts.Should().BeEmpty();
        result.Value.Summary.TotalModels.Should().Be(0);
    }

    [Fact]
    public async Task GetModelDriftReport_StableModel_ClassifiedAsStable()
    {
        var row = new GetModelDriftReport.ModelDriftRow(
            ModelA, "gpt-4o", "svc-1",
            InputDriftScore: 5.0, OutputDriftScore: 3.0, ConfidenceDrift: 0.01,
            NullRateIncrease: 0.0,
            Algorithm: GetModelDriftReport.DriftDetectionAlgorithm.PsiSimplified,
            Tier: GetModelDriftReport.ModelDriftTier.Stable,
            SampleCount: 500, DriftAcknowledged: false);

        var reader = Substitute.For<IModelDriftReader>();
        reader.GetDriftRowsAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetDriftTimelineAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateDriftHandler(reader);

        var result = await handler.Handle(
            new GetModelDriftReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetModelDriftReport.ModelDriftTier.Stable);
        result.Value.Summary.StableModels.Should().Be(1);
        result.Value.DriftAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetModelDriftReport_CriticalModel_AppearsInAlerts()
    {
        // With defaults: warning=20 input, 15 output. Critical = 3× = 60/45
        var row = new GetModelDriftReport.ModelDriftRow(
            ModelA, "model-x", "svc-1",
            InputDriftScore: 65.0, OutputDriftScore: 50.0, ConfidenceDrift: 0.15,
            NullRateIncrease: 5.0,
            Algorithm: GetModelDriftReport.DriftDetectionAlgorithm.PsiSimplified,
            Tier: GetModelDriftReport.ModelDriftTier.Stable, // will be reclassified
            SampleCount: 200, DriftAcknowledged: false);

        var reader = Substitute.For<IModelDriftReader>();
        reader.GetDriftRowsAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetDriftTimelineAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateDriftHandler(reader);

        var result = await handler.Handle(
            new GetModelDriftReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetModelDriftReport.ModelDriftTier.Critical);
        result.Value.Summary.CriticalModels.Should().Be(1);
        result.Value.DriftAlerts.Should().HaveCount(1);
        result.Value.DriftAlerts[0].ModelId.Should().Be(ModelA);
    }

    [Fact]
    public async Task GetModelDriftReport_AcknowledgedCritical_NotInAlerts()
    {
        var row = new GetModelDriftReport.ModelDriftRow(
            ModelA, "model-x", "svc-1",
            InputDriftScore: 65.0, OutputDriftScore: 50.0, ConfidenceDrift: 0.1,
            NullRateIncrease: 0.0,
            Algorithm: GetModelDriftReport.DriftDetectionAlgorithm.PsiSimplified,
            Tier: GetModelDriftReport.ModelDriftTier.Stable,
            SampleCount: 100, DriftAcknowledged: true); // already acknowledged

        var reader = Substitute.For<IModelDriftReader>();
        reader.GetDriftRowsAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetDriftTimelineAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateDriftHandler(reader);
        var result = await handler.Handle(new GetModelDriftReport.Query(TenantId), CancellationToken.None);

        result.Value.DriftAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetModelDriftReport_TopDriftingModelsTop5_ReturnsOrdered()
    {
        var rows = Enumerable.Range(1, 8).Select(i =>
            new GetModelDriftReport.ModelDriftRow(
                Guid.NewGuid(), $"model-{i}", "svc-1",
                InputDriftScore: i * 5.0, OutputDriftScore: 5.0,
                ConfidenceDrift: 0.0, NullRateIncrease: 0.0,
                Algorithm: GetModelDriftReport.DriftDetectionAlgorithm.PsiSimplified,
                Tier: GetModelDriftReport.ModelDriftTier.Stable,
                SampleCount: 100, DriftAcknowledged: false))
            .ToList();

        var reader = Substitute.For<IModelDriftReader>();
        reader.GetDriftRowsAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(rows);
        reader.GetDriftTimelineAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateDriftHandler(reader);
        var result = await handler.Handle(new GetModelDriftReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TopDriftingModels.Should().HaveCount(5);
        result.Value.Summary.TopDriftingModels[0].InputDriftScore.Should().Be(40.0);
    }

    [Theory]
    [InlineData("TenantId")]
    public async Task GetModelDriftReport_EmptyTenantId_ValidationFails(string fieldName)
    {
        var validator = new GetModelDriftReport.Validator();
        var q = new GetModelDriftReport.Query("", 30, 30);
        var result = await validator.ValidateAsync(q);
        result.IsValid.Should().BeFalse();
        _ = fieldName; // used for theory label
    }

    // ════════════════════════════════════════════════════════════════════════
    // AT.2 — GetAiModelQualityReport
    // ════════════════════════════════════════════════════════════════════════

    private GetAiModelQualityReport.Handler CreateQualityHandler(
        IAiModelQualityReader? reader = null)
    {
        reader ??= new NullAiModelQualityReader();
        return new GetAiModelQualityReport.Handler(
            reader, CreateConfigService(), CreateClock());
    }

    private static GetAiModelQualityReport.ModelQualityRow BuildQualityRow(
        Guid? modelId = null,
        double? accuracy = 98.0,
        double lowConf = 2.0,
        double? latencyP95 = 300.0,
        double fallback = 0.5,
        GetAiModelQualityReport.QualityTrend trend = GetAiModelQualityReport.QualityTrend.Stable,
        GetAiModelQualityReport.ModelQualityTier tier = GetAiModelQualityReport.ModelQualityTier.Excellent) =>
        new(modelId ?? ModelA, "model-x", "svc-1", 200,
            AccuracyRate: accuracy,
            FeedbackCoverageRate: accuracy.HasValue ? 80.0 : 0.0,
            AvgConfidenceScore: 0.92,
            LowConfidencePredictionRate: lowConf,
            InferenceLatencyP50Ms: 150.0,
            InferenceLatencyP95Ms: latencyP95,
            FallbackRate: fallback,
            Trend: trend,
            Tier: tier);

    [Fact]
    public async Task GetAiModelQualityReport_NoData_ReturnsEmptyReport()
    {
        var handler = CreateQualityHandler();
        var result = await handler.Handle(
            new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByModel.Should().BeEmpty();
        result.Value.Summary.TotalModels.Should().Be(0);
        result.Value.QualityAnomalies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAiModelQualityReport_ExcellentModel_ClassifiedCorrectly()
    {
        var row = BuildQualityRow(accuracy: 97.0, lowConf: 2.0, latencyP95: 300.0);

        var reader = Substitute.For<IAiModelQualityReader>();
        reader.GetQualityRowsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);

        var handler = CreateQualityHandler(reader);
        var result = await handler.Handle(new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiModelQualityReport.ModelQualityTier.Excellent);
        result.Value.Summary.ExcellentCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAiModelQualityReport_PoorModel_HighLowConfidence_ClassifiedCorrectly()
    {
        var row = BuildQualityRow(accuracy: 50.0, lowConf: 45.0, latencyP95: 2000.0);

        var reader = Substitute.For<IAiModelQualityReader>();
        reader.GetQualityRowsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);

        var handler = CreateQualityHandler(reader);
        var result = await handler.Handle(new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiModelQualityReport.ModelQualityTier.Poor);
        result.Value.Summary.PoorCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAiModelQualityReport_DegradingModel_AppearsInAnomalies()
    {
        var row = BuildQualityRow(
            trend: GetAiModelQualityReport.QualityTrend.Degrading,
            accuracy: 62.0, lowConf: 28.0);

        var reader = Substitute.For<IAiModelQualityReader>();
        reader.GetQualityRowsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);

        var handler = CreateQualityHandler(reader);
        var result = await handler.Handle(new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.Value.QualityAnomalies.Should().HaveCount(1);
        result.Value.QualityAnomalies[0].ModelId.Should().Be(ModelA);
    }

    [Fact]
    public async Task GetAiModelQualityReport_ModelWithoutFeedback_LowConfidenceCount()
    {
        var row = BuildQualityRow(accuracy: null, lowConf: 2.0) with
        {
            AvgConfidenceScore = 0.5 // below default 0.6 threshold
        };

        var reader = Substitute.For<IAiModelQualityReader>();
        reader.GetQualityRowsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);

        var handler = CreateQualityHandler(reader);
        var result = await handler.Handle(new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.LowConfidenceModelCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAiModelQualityReport_MultipleModels_TenantScoreWeighted()
    {
        var rows = new[]
        {
            BuildQualityRow(modelId: ModelA, accuracy: 97.0, lowConf: 2.0, latencyP95: 300.0),
            BuildQualityRow(modelId: ModelB, accuracy: 50.0, lowConf: 45.0, latencyP95: 2000.0)
        };

        var reader = Substitute.For<IAiModelQualityReader>();
        reader.GetQualityRowsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = CreateQualityHandler(reader);
        var result = await handler.Handle(new GetAiModelQualityReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TotalModels.Should().Be(2);
        result.Value.Summary.ExcellentCount.Should().Be(1);
        result.Value.Summary.PoorCount.Should().Be(1);
        result.Value.Summary.TenantAiQualityScore.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("")]
    public async Task GetAiModelQualityReport_EmptyTenantId_ValidationFails(string tenantId)
    {
        var validator = new GetAiModelQualityReport.Validator();
        var result = await validator.ValidateAsync(new GetAiModelQualityReport.Query(tenantId));
        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AT.3 — GetAiGovernanceComplianceReport
    // ════════════════════════════════════════════════════════════════════════

    private GetAiGovernanceComplianceReport.Handler CreateComplianceHandler(
        IAiGovernanceComplianceReader? reader = null)
    {
        reader ??= new NullAiGovernanceComplianceReader();
        return new GetAiGovernanceComplianceReport.Handler(
            reader, CreateConfigService(), CreateClock());
    }

    private static GetAiGovernanceComplianceReport.ModelComplianceRow BuildComplianceRow(
        Guid? modelId = null,
        bool hasApproval = true,
        bool hasAudit = true,
        double budgetCompliance = 98.0,
        double policyAdherence = 99.0,
        bool reviewOverdue = false,
        int budgetOverruns = 0,
        GetAiGovernanceComplianceReport.ModelGovernanceTier tier = GetAiGovernanceComplianceReport.ModelGovernanceTier.Compliant) =>
        new(modelId ?? ModelA, "model-x",
            HasFormalApproval: hasApproval,
            HasAuditTrail: hasAudit,
            BudgetComplianceRate: budgetCompliance,
            PolicyAdherence: policyAdherence,
            LastReviewDate: FixedNow.AddDays(-30),
            ReviewOverdue: reviewOverdue,
            BudgetOverrunPeriods: budgetOverruns,
            Tier: tier);

    [Fact]
    public async Task GetAiGovernanceComplianceReport_NoData_ReturnsZeroScore()
    {
        var handler = CreateComplianceHandler();
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByModel.Should().BeEmpty();
        result.Value.TenantAiGovernanceScore.Should().Be(0.0);
        result.Value.AiGovernanceComplianceIndex.Should().Be(0.0);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_FullyCompliantModel_ClassifiedCompliant()
    {
        var row = BuildComplianceRow(
            hasApproval: true, hasAudit: true, budgetCompliance: 98.0, reviewOverdue: false);

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiGovernanceComplianceReport.ModelGovernanceTier.Compliant);
        result.Value.TenantAiGovernanceScore.Should().Be(100.0);
        result.Value.AiGovernanceComplianceIndex.Should().Be(100.0);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_MissingApproval_ClassifiedNonCompliant()
    {
        var row = BuildComplianceRow(
            hasApproval: false, hasAudit: true, budgetCompliance: 98.0, reviewOverdue: false);

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiGovernanceComplianceReport.ModelGovernanceTier.NonCompliant);
        result.Value.Gaps.ModelsWithoutApproval.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_NoGovernanceData_ClassifiedUntracked()
    {
        var row = BuildComplianceRow(
            hasApproval: false, hasAudit: false, budgetCompliance: 0.0, policyAdherence: 0.0);

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiGovernanceComplianceReport.ModelGovernanceTier.Untracked);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_ReviewOverdue_ClassifiedPartial()
    {
        var row = BuildComplianceRow(
            hasApproval: true, hasAudit: true, budgetCompliance: 98.0, reviewOverdue: true);

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByModel[0].Tier.Should().Be(GetAiGovernanceComplianceReport.ModelGovernanceTier.Partial);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_BudgetOverrunAboveThreshold_AppearsInGaps()
    {
        var row = BuildComplianceRow(
            hasApproval: true, hasAudit: true, budgetCompliance: 70.0,
            reviewOverdue: false, budgetOverruns: 3); // default threshold = 2

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([row]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.Gaps.BudgetOverrunModels.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_PolicyViolations_AppearInGaps()
    {
        var violation = new GetAiGovernanceComplianceReport.PolicyViolation(
            ModelA, "model-x", "RoleViolation", 5, FixedNow.AddHours(-2));

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([violation]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.Gaps.PolicyViolatingCalls.Should().HaveCount(1);
        result.Value.Gaps.PolicyViolatingCalls[0].ViolationType.Should().Be("RoleViolation");
    }

    [Fact]
    public async Task GetAiGovernanceComplianceReport_MixedModels_ComplianceIndexOnlyCompliant()
    {
        var compliant = BuildComplianceRow(modelId: ModelA, hasApproval: true, hasAudit: true,
            budgetCompliance: 98.0, reviewOverdue: false);
        var nonCompliant = BuildComplianceRow(modelId: ModelB, hasApproval: false, hasAudit: false,
            budgetCompliance: 0.0, policyAdherence: 0.0);

        var reader = Substitute.For<IAiGovernanceComplianceReader>();
        reader.GetComplianceRowsAsync(TenantId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([compliant, nonCompliant]);
        reader.GetPolicyViolationsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateComplianceHandler(reader);
        var result = await handler.Handle(
            new GetAiGovernanceComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.AiGovernanceComplianceIndex.Should().Be(50.0); // 1/2 = 50%
        result.Value.ByModel.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    public async Task GetAiGovernanceComplianceReport_EmptyTenantId_ValidationFails(string tenantId)
    {
        var validator = new GetAiGovernanceComplianceReport.Validator();
        var result = await validator.ValidateAsync(new GetAiGovernanceComplianceReport.Query(tenantId));
        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AT.1 — ModelPredictionSample domain entity
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ModelPredictionSample_Create_SetsAllFields()
    {
        var sample = ModelPredictionSample.Create(
            ModelA, "gpt-4o", "svc-1", TenantId, FixedNow,
            "{\"f\":0.5}", "ClassA", 0.92, 120, "ClassA", false);

        sample.ModelId.Should().Be(ModelA);
        sample.ModelName.Should().Be("gpt-4o");
        sample.ServiceId.Should().Be("svc-1");
        sample.TenantId.Should().Be(TenantId);
        sample.ConfidenceScore.Should().Be(0.92);
        sample.InferenceLatencyMs.Should().Be(120);
        sample.ActualClass.Should().Be("ClassA");
        sample.IsFallback.Should().BeFalse();
        sample.DriftAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void ModelPredictionSample_AcknowledgeDrift_SetsFlagTrue()
    {
        var sample = ModelPredictionSample.Create(
            ModelA, "gpt-4o", "svc-1", TenantId, FixedNow, null, null, 0.5, null, null, false);

        sample.AcknowledgeDrift();

        sample.DriftAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void ModelPredictionSample_Id_IsUnique()
    {
        var s1 = ModelPredictionSample.Create(ModelA, "m", "s", TenantId, FixedNow, null, null, null, null, null, false);
        var s2 = ModelPredictionSample.Create(ModelA, "m", "s", TenantId, FixedNow, null, null, null, null, null, false);

        s1.Id.Should().NotBe(s2.Id);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Null infrastructure
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NullModelPredictionRepository_AllMethods_ReturnEmptyOrComplete()
    {
        var repo = new NullModelPredictionRepository();
        var sample = ModelPredictionSample.Create(ModelA, "m", "s", TenantId, FixedNow, null, null, null, null, null, false);

        await repo.AddAsync(sample, CancellationToken.None); // should not throw
        var list = await repo.ListByModelAsync(ModelA, TenantId, FixedNow.AddDays(-30), FixedNow, CancellationToken.None);
        var count = await repo.CountByModelAsync(ModelA, TenantId, FixedNow.AddDays(-30), CancellationToken.None);

        list.Should().BeEmpty();
        count.Should().Be(0);
    }

    [Fact]
    public async Task NullModelDriftReader_ReturnsEmpty()
    {
        var reader = new NullModelDriftReader();
        var rows = await reader.GetDriftRowsAsync(TenantId, FixedNow.AddDays(-60), FixedNow.AddDays(-30),
            FixedNow.AddDays(-30), FixedNow, CancellationToken.None);
        var timeline = await reader.GetDriftTimelineAsync(ModelA, TenantId, FixedNow.AddDays(-30), FixedNow, CancellationToken.None);

        rows.Should().BeEmpty();
        timeline.Should().BeEmpty();
    }

    [Fact]
    public async Task NullAiModelQualityReader_ReturnsEmpty()
    {
        var reader = new NullAiModelQualityReader();
        var rows = await reader.GetQualityRowsAsync(TenantId, FixedNow.AddDays(-30), FixedNow, 100, CancellationToken.None);
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task NullAiGovernanceComplianceReader_ReturnsEmpty()
    {
        var reader = new NullAiGovernanceComplianceReader();
        var compliance = await reader.GetComplianceRowsAsync(TenantId, 30, 90, CancellationToken.None);
        var violations = await reader.GetPolicyViolationsAsync(TenantId, FixedNow.AddDays(-30), FixedNow, CancellationToken.None);

        compliance.Should().BeEmpty();
        violations.Should().BeEmpty();
    }
}
