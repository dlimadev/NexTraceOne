using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentDriftReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature GetEnvironmentDriftReport.
/// Verificam: sucesso (encontrado), relatório não encontrado.
/// </summary>
public sealed class GetEnvironmentDriftReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IEnvironmentDriftReportRepository _reportRepo = Substitute.For<IEnvironmentDriftReportRepository>();

    [Fact]
    public async Task Handle_ReportExists_ShouldReturnFullReport()
    {
        var report = EnvironmentDriftReport.Generate(
            "production", "staging", "ServiceVersions,Configurations",
            "[{\"service\":\"order-svc\"}]", "[{\"key\":\"timeout\"}]", null, null, null,
            "[{\"action\":\"align versions\"}]",
            3, 1, DriftSeverity.Critical, TenantId, FixedNow);

        _reportRepo.GetByIdAsync(Arg.Any<EnvironmentDriftReportId>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var handler = new GetEnvironmentDriftReport.Handler(_reportRepo);
        var query = new GetEnvironmentDriftReport.Query(report.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceEnvironment.Should().Be("production");
        result.Value.TargetEnvironment.Should().Be("staging");
        result.Value.ServiceVersionDrifts.Should().Contain("order-svc");
        result.Value.ConfigurationDrifts.Should().Contain("timeout");
        result.Value.Recommendations.Should().Contain("align versions");
        result.Value.TotalDriftItems.Should().Be(3);
        result.Value.CriticalDriftItems.Should().Be(1);
        result.Value.OverallSeverity.Should().Be("Critical");
        result.Value.Status.Should().Be("Generated");
    }

    [Fact]
    public async Task Handle_ReportNotFound_ShouldReturnError()
    {
        _reportRepo.GetByIdAsync(Arg.Any<EnvironmentDriftReportId>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new GetEnvironmentDriftReport.Handler(_reportRepo);
        var query = new GetEnvironmentDriftReport.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyReportId()
    {
        var validator = new GetEnvironmentDriftReport.Validator();
        var query = new GetEnvironmentDriftReport.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidQuery()
    {
        var validator = new GetEnvironmentDriftReport.Validator();
        var query = new GetEnvironmentDriftReport.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
