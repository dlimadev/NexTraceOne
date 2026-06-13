using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.EvaluateTemplateQualityGates;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;
using NexTraceOne.Catalog.Domain.Templates.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts;

/// <summary>
/// Testes unitários para AQ.3 — EvaluateTemplateQualityGates.
/// Garante que os quality gates declarados no template são confrontados de forma
/// determinística com os dados reais de qualidade de código (SonarQube).
/// </summary>
public sealed class EvaluateTemplateQualityGatesTests
{
    private readonly IServiceTemplateRepository _templateRepo = Substitute.For<IServiceTemplateRepository>();
    private readonly ICodeQualityRepository _codeQualityRepo = Substitute.For<ICodeQualityRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private const string TenantId = "tenant-1";
    private const string ServiceId = "svc-payments";
    private readonly DateTimeOffset _now = new(2026, 6, 13, 10, 0, 0, TimeSpan.Zero);

    public EvaluateTemplateQualityGatesTests() => _clock.UtcNow.Returns(_now);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ServiceTemplate TemplateWithCoverageGate(int minimumCoverage)
    {
        var template = ServiceTemplate.Create(
            slug: "dotnet-rest-api",
            displayName: ".NET REST API",
            description: "Standard REST API template.",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.DotNet,
            defaultDomain: "payments",
            defaultTeam: "platform-team");

        var manifest = new TemplateManifestV2
        {
            Architecture = new ManifestArchitecture { Pattern = "Clean" },
            QualityGates = new ManifestQualityGates
            {
                TestCoverageMinimum = minimumCoverage,
                RequireUnitTests = true,
                RequireOpenApiSpec = true,
                RequiredLinters = new[] { "StyleCop" }
            }
        };
        template.SetArchitecturePattern(manifest.ToJson());
        return template;
    }

    private CodeQualityRecord Record(double coverage, string qualityGateStatus, int bugs = 0, int vulnerabilities = 0, int codeSmells = 0)
        => new(
            Id: Guid.NewGuid(),
            TenantId: TenantId,
            ServiceId: ServiceId,
            ServiceName: "payments",
            ProjectKey: "payments-key",
            QualityGateStatus: qualityGateStatus,
            Coverage: coverage,
            Bugs: bugs,
            Vulnerabilities: vulnerabilities,
            CodeSmells: codeSmells,
            DuplicatedLinesDensity: 0,
            Branch: "main",
            AnalyzedAt: _now);

    private EvaluateTemplateQualityGates.Handler CreateHandler()
        => new(_templateRepo, _codeQualityRepo, _clock);

    private EvaluateTemplateQualityGates.Query QueryBySlug(string slug = "dotnet-rest-api")
        => new(ServiceId, TenantId, TemplateId: null, TemplateSlug: slug);

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CoverageBelowMinimum_FailsWithCoverageBreach()
    {
        _templateRepo.GetBySlugAsync("dotnet-rest-api", Arg.Any<CancellationToken>())
            .Returns(TemplateWithCoverageGate(70));
        _codeQualityRepo.GetLatestAsync(ServiceId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Record(coverage: 55, qualityGateStatus: "OK"));

        var result = await CreateHandler().Handle(QueryBySlug(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Status.Should().Be(EvaluateTemplateQualityGates.Statuses.Failed);
        result.Value.Breaches.Should().ContainSingle(b => b.Gate == "coverage");
        result.Value.RequiredCoverage.Should().Be(70);
        result.Value.ActualCoverage.Should().Be(55);
    }

    [Fact]
    public async Task Handle_CoverageMeetsMinimumAndGateGreen_Passes()
    {
        _templateRepo.GetBySlugAsync("dotnet-rest-api", Arg.Any<CancellationToken>())
            .Returns(TemplateWithCoverageGate(70));
        _codeQualityRepo.GetLatestAsync(ServiceId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Record(coverage: 82, qualityGateStatus: "OK"));

        var result = await CreateHandler().Handle(QueryBySlug(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Status.Should().Be(EvaluateTemplateQualityGates.Statuses.Passed);
        result.Value.Breaches.Should().BeEmpty();
        result.Value.DeclaredRequirements.Should().Contain("Coverage >= 70%");
        result.Value.DeclaredRequirements.Should().Contain("Linters: StyleCop");
    }

    [Fact]
    public async Task Handle_SonarQualityGateFailing_FailsWithGateBreach()
    {
        _templateRepo.GetBySlugAsync("dotnet-rest-api", Arg.Any<CancellationToken>())
            .Returns(TemplateWithCoverageGate(70));
        _codeQualityRepo.GetLatestAsync(ServiceId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Record(coverage: 90, qualityGateStatus: "ERROR"));

        var result = await CreateHandler().Handle(QueryBySlug(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Breaches.Should().ContainSingle(b => b.Gate == "sonar_quality_gate");
    }

    [Fact]
    public async Task Handle_NoCodeQualityRecord_ReturnsNoQualityData()
    {
        _templateRepo.GetBySlugAsync("dotnet-rest-api", Arg.Any<CancellationToken>())
            .Returns(TemplateWithCoverageGate(70));
        _codeQualityRepo.GetLatestAsync(ServiceId, TenantId, Arg.Any<CancellationToken>())
            .Returns((CodeQualityRecord?)null);

        var result = await CreateHandler().Handle(QueryBySlug(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Status.Should().Be(EvaluateTemplateQualityGates.Statuses.NoQualityData);
        result.Value.RequiredCoverage.Should().Be(70);
        result.Value.ActualCoverage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TemplateWithoutManifest_ReturnsNoGatesDefined()
    {
        var template = ServiceTemplate.Create(
            "kafka-consumer", "Kafka Consumer", "Event consumer.", "1.0.0",
            TemplateServiceType.EventDriven, TemplateLanguage.DotNet, "events", "integration-team");
        _templateRepo.GetBySlugAsync("kafka-consumer", Arg.Any<CancellationToken>()).Returns(template);

        var result = await CreateHandler().Handle(QueryBySlug("kafka-consumer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Status.Should().Be(EvaluateTemplateQualityGates.Statuses.NoGatesDefined);
        await _codeQualityRepo.DidNotReceive().GetLatestAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TemplateNotFound_ReturnsNotFound()
    {
        _templateRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceTemplate?)null);

        var result = await CreateHandler().Handle(QueryBySlug("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }
}
