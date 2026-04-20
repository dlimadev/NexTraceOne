using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

using GetObservabilityScoreFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetObservabilityScore.GetObservabilityScore;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para GetObservabilityScore — score de maturidade de observabilidade de serviço.
/// Valida happy path, perfil não encontrado e validação de entrada.
/// </summary>
public sealed class GetObservabilityScoreTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 16, 0, 0, TimeSpan.Zero);

    private readonly IObservabilityProfileRepository _repo = Substitute.For<IObservabilityProfileRepository>();

    private GetObservabilityScoreFeature.Handler CreateHandler() => new(_repo);

    private static ObservabilityProfile MakeProfile(
        string service = "order-api",
        string env = "production",
        bool hasTracing = true,
        bool hasMetrics = true,
        bool hasLogging = true,
        bool hasAlerting = true,
        bool hasDashboard = false) =>
        ObservabilityProfile.Assess(service, env, hasTracing, hasMetrics, hasLogging, hasAlerting, hasDashboard, FixedNow);

    // ── Happy path — full observability ──────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileFound_ReturnsMappedResponse()
    {
        var profile = MakeProfile();

        _repo.GetByServiceAndEnvironmentAsync("order-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await CreateHandler().Handle(
            new GetObservabilityScoreFeature.Query("order-api", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("order-api");
        result.Value.Environment.Should().Be("production");
        result.Value.HasTracing.Should().BeTrue();
        result.Value.HasMetrics.Should().BeTrue();
        result.Value.HasLogging.Should().BeTrue();
        result.Value.HasAlerting.Should().BeTrue();
        result.Value.HasDashboard.Should().BeFalse();
        result.Value.ObservabilityScore.Should().BeInRange(0m, 1m);
        result.Value.LastAssessedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_AllCapabilitiesPresent_ScoreIsMaximum()
    {
        var profile = MakeProfile(hasTracing: true, hasMetrics: true, hasLogging: true, hasAlerting: true, hasDashboard: true);

        _repo.GetByServiceAndEnvironmentAsync("order-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await CreateHandler().Handle(
            new GetObservabilityScoreFeature.Query("order-api", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservabilityScore.Should().Be(1.00m);
    }

    [Fact]
    public async Task Handle_NoCapabilitiesPresent_ScoreIsMinimum()
    {
        var profile = MakeProfile(hasTracing: false, hasMetrics: false, hasLogging: false, hasAlerting: false, hasDashboard: false);

        _repo.GetByServiceAndEnvironmentAsync("order-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await CreateHandler().Handle(
            new GetObservabilityScoreFeature.Query("order-api", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservabilityScore.Should().Be(0.00m);
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsFailure()
    {
        _repo.GetByServiceAndEnvironmentAsync("unknown-svc", "staging", Arg.Any<CancellationToken>())
            .Returns((ObservabilityProfile?)null);

        var result = await CreateHandler().Handle(
            new GetObservabilityScoreFeature.Query("unknown-svc", "staging"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "production")]
    [InlineData("order-api", "")]
    public void Validator_MissingRequiredField_ReturnsError(string serviceName, string environment)
    {
        var validator = new GetObservabilityScoreFeature.Validator();
        var result = validator.Validate(new GetObservabilityScoreFeature.Query(serviceName, environment));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidInput_Passes()
    {
        var validator = new GetObservabilityScoreFeature.Validator();
        var result = validator.Validate(new GetObservabilityScoreFeature.Query("checkout-api", "staging"));

        result.IsValid.Should().BeTrue();
    }
}
