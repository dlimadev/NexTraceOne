using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetSecretsExposureRiskReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AD.2 — GetSecretsExposureRiskReport.
/// Cobre detecção por categoria de regex, ExposureRisk, filtro por MinRiskLevel,
/// distribuição por categoria, TopServicesAtRisk e Validator.
/// </summary>
public sealed class WaveAdSecretsExposureRiskReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ad2";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetSecretsExposureRiskReport.Handler CreateHandler(
        IReadOnlyList<ArtifactTextEntry> entries)
    {
        var reader = Substitute.For<ISecretsExposureReader>();
        reader.ListArtifactTextsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetSecretsExposureRiskReport.Handler(reader, CreateClock());
    }

    private static ArtifactTextEntry MakeEntry(string id, string svc, string content, string type = "Contract")
        => new(id, type, svc, $"title-{id}", content);

    private static GetSecretsExposureRiskReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── 1. Tenant sem artefactos devolve relatório vazio ──────────────────

    [Fact]
    public async Task Handle_NoArtifacts_ReturnsEmptyReport()
    {
        var result = await CreateHandler([]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalScanned.Should().Be(0);
        result.Value.TotalAtRisk.Should().Be(0);
        result.Value.AffectedArtifacts.Should().BeEmpty();
    }

    // ── 2. Conteúdo sem padrões não gera entradas no relatório ───────────

    [Fact]
    public async Task Handle_CleanContent_NoAffectedArtifacts()
    {
        var entry = MakeEntry("a1", "svc-clean", "This contract has no secrets at all.");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalScanned.Should().Be(1);
        result.Value.TotalAtRisk.Should().Be(0);
    }

    // ── 3. ApiKey detectado como Critical ────────────────────────────────

    [Fact]
    public async Task Handle_ApiKeyPattern_DetectedAsCritical()
    {
        var entry = MakeEntry("a2", "svc-apikey",
            "Use the token AKIAIOSFODNN7EXAMPLE for AWS access.");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(),  CancellationToken.None);

        result.Value.TotalAtRisk.Should().Be(1);
        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.Critical);
        artifact.DetectedCategories.Should().Contain(GetSecretsExposureRiskReport.ExposureCategory.ApiKey);
    }

    // ── 4. JWT Token detectado como High ─────────────────────────────────

    [Fact]
    public async Task Handle_JwtTokenPattern_DetectedAsHigh()
    {
        var entry = MakeEntry("a3", "svc-jwt",
            "Example header: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.High);
        artifact.DetectedCategories.Should().Contain(GetSecretsExposureRiskReport.ExposureCategory.JwtToken);
    }

    // ── 5. ConnectionString detectada como High ───────────────────────────

    [Fact]
    public async Task Handle_ConnectionStringPattern_DetectedAsHigh()
    {
        var entry = MakeEntry("a4", "svc-connstr",
            "Connection: Server=db;password=MyS3cretPass;Database=app");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.High);
        artifact.DetectedCategories.Should().Contain(GetSecretsExposureRiskReport.ExposureCategory.ConnectionString);
    }

    // ── 6. IP privado detectado como Low ──────────────────────────────────

    [Fact]
    public async Task Handle_PrivateIpPattern_DetectedAsLow()
    {
        var entry = MakeEntry("a5", "svc-ip", "Service endpoint at 192.168.10.5:8080");
        var query = new GetSecretsExposureRiskReport.Query(TenantId: TenantId, MinRiskLevel: GetSecretsExposureRiskReport.ExposureRisk.Low);
        var result = await CreateHandler([entry]).Handle(query, CancellationToken.None);

        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.Low);
        artifact.DetectedCategories.Should().Contain(GetSecretsExposureRiskReport.ExposureCategory.PrivateIp);
    }

    // ── 7. Email pessoal detectado como Medium ────────────────────────────

    [Fact]
    public async Task Handle_PersonalEmailPattern_DetectedAsMedium()
    {
        var entry = MakeEntry("a6", "svc-email",
            "Contact john.doe@gmail.com for API access.");
        var query = new GetSecretsExposureRiskReport.Query(TenantId: TenantId, MinRiskLevel: GetSecretsExposureRiskReport.ExposureRisk.Medium);
        var result = await CreateHandler([entry]).Handle(query, CancellationToken.None);

        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.Medium);
        artifact.DetectedCategories.Should().Contain(GetSecretsExposureRiskReport.ExposureCategory.PersonalEmail);
    }

    // ── 8. MinRiskLevel filtra artefactos abaixo do threshold ────────────

    [Fact]
    public async Task Handle_MinRiskLevelHigh_FiltersOutLowAndMedium()
    {
        var lowEntry = MakeEntry("a7", "svc-low", "Server at 10.0.0.1");
        var critEntry = MakeEntry("a8", "svc-crit", "Token: ghp_" + new string('a', 36));
        var query = new GetSecretsExposureRiskReport.Query(TenantId: TenantId, MinRiskLevel: GetSecretsExposureRiskReport.ExposureRisk.High);
        var result = await CreateHandler([lowEntry, critEntry]).Handle(query, CancellationToken.None);

        result.Value.TotalScanned.Should().Be(2);
        result.Value.TotalAtRisk.Should().Be(1);
        result.Value.AffectedArtifacts.Single().ArtifactId.Should().Be("a8");
    }

    // ── 9. Múltiplas categorias num artefacto — risco é o mais elevado ────

    [Fact]
    public async Task Handle_MultiplePatterns_RiskIsMaximum()
    {
        // ApiKey (Critical) + personal email (Medium) — resultado: Critical
        var entry = MakeEntry("a9", "svc-multi",
            "Contact admin@hotmail.com — API key: AKIAIOSFODNN7EXAMPLE");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var artifact = result.Value.AffectedArtifacts.Single();
        artifact.ExposureRisk.Should().Be(GetSecretsExposureRiskReport.ExposureRisk.Critical);
        artifact.DetectedCategories.Count.Should().BeGreaterThan(1);
    }

    // ── 10. TopServicesAtRisk ordena por risco e depois por frequência ─────

    [Fact]
    public async Task Handle_MultipleArtifacts_TopServicesOrderedByRiskDescending()
    {
        var entries = new[]
        {
            MakeEntry("b1", "svc-low", "Server: 10.0.0.1"),
            MakeEntry("b2", "svc-crit", "Token: AKIAIOSFODNN7EXAMPLE"),
            MakeEntry("b3", "svc-crit", "Another: AKIAIOSFODNN7EXAMPLEB")
        };
        var query = new GetSecretsExposureRiskReport.Query(TenantId: TenantId, MinRiskLevel: GetSecretsExposureRiskReport.ExposureRisk.Low);
        var result = await CreateHandler(entries).Handle(query, CancellationToken.None);

        result.Value.TopServicesAtRisk.First().Should().Be("svc-crit");
    }

    // ── 11. CategoryDistribution conta por categoria ──────────────────────

    [Fact]
    public async Task Handle_TwoApiKeyArtifacts_CategoryDistributionReflectsCount()
    {
        var entries = new[]
        {
            MakeEntry("c1", "svc-a", "Key: AKIAIOSFODNN7EXAMPLE"),
            MakeEntry("c2", "svc-b", "Key: AKIAIOSFODNN7EXAMPLE2A")
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.CategoryDistribution["ApiKey"].Should().Be(2);
    }

    // ── 12. Conteúdo com placeholder não é detectado como ConnectionString ─

    [Fact]
    public async Task Handle_PlaceholderConnectionString_NotDetected()
    {
        var entry = MakeEntry("d1", "svc-doc",
            "Example: password=placeholder or secret=changeme");
        var query = new GetSecretsExposureRiskReport.Query(TenantId: TenantId, MinRiskLevel: GetSecretsExposureRiskReport.ExposureRisk.Low);
        var result = await CreateHandler([entry]).Handle(query, CancellationToken.None);

        result.Value.TotalAtRisk.Should().Be(0);
    }

    // ── 13. Validator — TenantId obrigatório ─────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_FailsValidation()
    {
        var validator = new GetSecretsExposureRiskReport.Validator();
        var result = validator.Validate(new GetSecretsExposureRiskReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── 14. Validator — MaxArtifacts fora do intervalo inválido ──────────

    [Fact]
    public void Validator_MaxArtifactsOutOfRange_FailsValidation()
    {
        var validator = new GetSecretsExposureRiskReport.Validator();
        var result = validator.Validate(new GetSecretsExposureRiskReport.Query(TenantId, MaxArtifacts: 0));
        result.IsValid.Should().BeFalse();
    }
}
