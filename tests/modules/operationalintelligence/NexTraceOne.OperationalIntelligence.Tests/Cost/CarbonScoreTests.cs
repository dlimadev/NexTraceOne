using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCarbonScoreReport;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

public sealed class CarbonScoreTests
{
    private readonly ICarbonScoreRepository _repository = Substitute.For<ICarbonScoreRepository>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CarbonScoreTests()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _clock.UtcToday.Returns(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void CarbonScoreRecord_Create_CalculatesFormula()
    {
        var record = CarbonScoreRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            cpuHours: 10.0, memoryGbHours: 5.0, networkGb: 1.0,
            intensityFactor: 233.0);

        // 10 × 233 + 5 × 0.392 + 1 × 60 = 2330 + 1.96 + 60 = 2391.96
        record.CarbonGrams.Should().BeApproximately(2391.96, precision: 0.01);
    }

    [Fact]
    public void CarbonScoreRecord_Create_SetsAllFields()
    {
        var serviceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var record = CarbonScoreRecord.Create(serviceId, tenantId, date, 1.0, 2.0, 0.5, 233.0);

        record.ServiceId.Should().Be(serviceId);
        record.TenantId.Should().Be(tenantId);
        record.Date.Should().Be(date);
        record.CpuHours.Should().Be(1.0);
        record.MemoryGbHours.Should().Be(2.0);
        record.NetworkGb.Should().Be(0.5);
        record.IntensityFactor.Should().Be(233.0);
        record.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void CarbonScoreRecord_ZeroMetrics_HasZeroCarbonGrams()
    {
        var record = CarbonScoreRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            0.0, 0.0, 0.0, 233.0);

        record.CarbonGrams.Should().Be(0.0);
    }

    [Fact]
    public void CarbonScoreRecord_Create_NegativeIntensityFactor_Throws()
    {
        var act = () => CarbonScoreRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            1.0, 1.0, 1.0, -1.0);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GetCarbonScoreReport_ReturnsAggregatedTotals()
    {
        var serviceId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = new List<CarbonScoreRecord>
        {
            CarbonScoreRecord.Create(serviceId, _tenant.Id, today, 10.0, 5.0, 1.0, 233.0),
            CarbonScoreRecord.Create(serviceId, _tenant.Id, today.AddDays(-1), 5.0, 2.0, 0.5, 233.0),
        };
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var result = await handler.Handle(new GetCarbonScoreReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCarbonGrams.Should().BeGreaterThan(0);
        result.Value.TopServices.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCarbonScoreReport_EmptyRecords_ReturnsTotalsOfZero()
    {
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<CarbonScoreRecord>());

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var result = await handler.Handle(new GetCarbonScoreReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCarbonGrams.Should().Be(0.0);
        result.Value.TopServices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCarbonScoreReport_MultipleServices_Top5Only()
    {
        var records = Enumerable.Range(1, 8).Select(i =>
            CarbonScoreRecord.Create(Guid.NewGuid(), _tenant.Id,
                DateOnly.FromDateTime(DateTime.UtcNow),
                i * 10.0, i * 2.0, i * 0.5, 233.0)).ToList();
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var result = await handler.Handle(new GetCarbonScoreReport.Query(), CancellationToken.None);

        result.Value.TopServices.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetCarbonScoreReport_DefaultPeriodIs30Days()
    {
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<CarbonScoreRecord>());

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var today = _clock.UtcToday;
        var result = await handler.Handle(new GetCarbonScoreReport.Query(), CancellationToken.None);

        result.Value.From.Should().Be(today.AddDays(-30));
        result.Value.To.Should().Be(today);
    }

    [Fact]
    public void CarbonScoreRecord_Create_EmptyServiceId_Throws()
    {
        var act = () => CarbonScoreRecord.Create(
            Guid.Empty, Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            1.0, 1.0, 1.0, 233.0);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GetCarbonScoreReport_CustomPeriod_PassedToRepository()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, from, to, Arg.Any<CancellationToken>())
            .Returns(new List<CarbonScoreRecord>());

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var result = await handler.Handle(new GetCarbonScoreReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).ListByTenantAndPeriodAsync(
            _tenant.Id, from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCarbonScoreReport_TopServicesSortedByEmissions()
    {
        var records = new List<CarbonScoreRecord>
        {
            CarbonScoreRecord.Create(Guid.NewGuid(), _tenant.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1.0, 0, 0, 233),
            CarbonScoreRecord.Create(Guid.NewGuid(), _tenant.Id, DateOnly.FromDateTime(DateTime.UtcNow), 100.0, 0, 0, 233),
        };
        _repository.ListByTenantAndPeriodAsync(_tenant.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetCarbonScoreReport.Handler(_repository, _tenant, _clock);
        var result = await handler.Handle(new GetCarbonScoreReport.Query(), CancellationToken.None);

        // Primeiro deve ter mais emissões (100 × 233 > 1 × 233)
        result.Value.TopServices[0].TotalCarbonGrams.Should().BeGreaterThan(
            result.Value.TopServices[1].TotalCarbonGrams);
    }
}
