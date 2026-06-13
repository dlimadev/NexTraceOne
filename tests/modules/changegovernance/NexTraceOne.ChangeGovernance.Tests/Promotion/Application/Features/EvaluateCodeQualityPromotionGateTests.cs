using NexTraceOne.Catalog.Contracts.Quality.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateCodeQualityPromotionGate;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application.Features;

/// <summary>
/// Testes para EvaluateCodeQualityPromotionGate — mapeamento (aprovação × modo de enforcement)
/// para (decisão, bloqueio). Valida que apenas HardEnforce com gate reprovado bloqueia a promoção.
/// </summary>
public sealed class EvaluateCodeQualityPromotionGateTests
{
    private readonly ICatalogQualityGateModule _qualityGate = Substitute.For<ICatalogQualityGateModule>();

    private const string ServiceId = "payments";
    private const string TenantId = "tenant-1";

    private void GateReturns(bool passed, string status = "Failed")
        => _qualityGate.EvaluateAsync(ServiceId, TenantId, Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TemplateQualityGateResult(
                ServiceId: ServiceId,
                Status: passed ? "Passed" : status,
                Passed: passed,
                RequiredCoverage: 70,
                ActualCoverage: passed ? 85 : 50,
                SonarQualityGateStatus: passed ? "OK" : "ERROR",
                Breaches: passed ? Array.Empty<string>() : new[] { "Coverage 50% is below the required minimum of 70%." }));

    private async Task<EvaluateCodeQualityPromotionGate.Verdict> Evaluate(CodeQualityGateEnforcement enforcement)
    {
        var handler = new EvaluateCodeQualityPromotionGate.Handler(_qualityGate);
        var result = await handler.Handle(
            new EvaluateCodeQualityPromotionGate.Query(ServiceId, TenantId, enforcement), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task GatePassed_NeverBlocks()
    {
        GateReturns(passed: true);

        var verdict = await Evaluate(CodeQualityGateEnforcement.HardEnforce);

        verdict.GatePassed.Should().BeTrue();
        verdict.Blocking.Should().BeFalse();
        verdict.Decision.Should().Be(EvaluateCodeQualityPromotionGate.Decisions.Pass);
    }

    [Fact]
    public async Task GateFailed_Advisory_DoesNotBlock()
    {
        GateReturns(passed: false);

        var verdict = await Evaluate(CodeQualityGateEnforcement.Advisory);

        verdict.Blocking.Should().BeFalse();
        verdict.Decision.Should().Be(EvaluateCodeQualityPromotionGate.Decisions.Advisory);
    }

    [Fact]
    public async Task GateFailed_SoftEnforce_WarnsButDoesNotBlock()
    {
        GateReturns(passed: false);

        var verdict = await Evaluate(CodeQualityGateEnforcement.SoftEnforce);

        verdict.Blocking.Should().BeFalse();
        verdict.Decision.Should().Be(EvaluateCodeQualityPromotionGate.Decisions.Warn);
    }

    [Fact]
    public async Task GateFailed_HardEnforce_Blocks()
    {
        GateReturns(passed: false);

        var verdict = await Evaluate(CodeQualityGateEnforcement.HardEnforce);

        verdict.Blocking.Should().BeTrue();
        verdict.Decision.Should().Be(EvaluateCodeQualityPromotionGate.Decisions.Block);
        verdict.Breaches.Should().NotBeEmpty();
    }
}
