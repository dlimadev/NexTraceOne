using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListEnvironmentDriftReports;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature ListEnvironmentDriftReports.
/// Verificam: lista vazia, lista com resultados, filtros aplicados.
/// </summary>
public sealed class ListEnvironmentDriftReportsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IEnvironmentDriftReportRepository _reportRepo = Substitute.For<IEnvironmentDriftReportRepository>();

    [Fact]
    public async Task Handle_NoReports_ShouldReturnEmptyList()
    {
        _reportRepo.ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DriftReportStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentDriftReport>());

        var handler = new ListEnvironmentDriftReports.Handler(_reportRepo);
        var query = new ListEnvironmentDriftReports.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reports.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithReports_ShouldReturnList()
    {
        var report1 = EnvironmentDriftReport.Generate(
            "production", "staging", "ServiceVersions",
            "[]", null, null, null, null, null,
            2, 0, DriftSeverity.Medium, TenantId, FixedNow);
        var report2 = EnvironmentDriftReport.Generate(
            "production", "dev", "Configurations",
            null, "[]", null, null, null, null,
            5, 1, DriftSeverity.Critical, TenantId, FixedNow.AddHours(1));

        _reportRepo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentDriftReport> { report1, report2 });

        var handler = new ListEnvironmentDriftReports.Handler(_reportRepo);
        var query = new ListEnvironmentDriftReports.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reports.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldParseAndPassToRepo()
    {
        _reportRepo.ListAsync(null, null, DriftReportStatus.Generated, Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentDriftReport>());

        var handler = new ListEnvironmentDriftReports.Handler(_reportRepo);
        var query = new ListEnvironmentDriftReports.Query(Status: "Generated");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _reportRepo.Received(1).ListAsync(null, null, DriftReportStatus.Generated, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEnvironmentFilters_ShouldPassToRepo()
    {
        _reportRepo.ListAsync("production", "staging", null, Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentDriftReport>());

        var handler = new ListEnvironmentDriftReports.Handler(_reportRepo);
        var query = new ListEnvironmentDriftReports.Query(SourceEnvironment: "production", TargetEnvironment: "staging");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _reportRepo.Received(1).ListAsync("production", "staging", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldAcceptEmptyQuery()
    {
        var validator = new ListEnvironmentDriftReports.Validator();
        var query = new ListEnvironmentDriftReports.Query();

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
