using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ImportCostBatch;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Domain;

/// <summary>Testes unitários da entidade CostImportBatch — criação, transições de estado e validação.</summary>
public sealed class CostImportBatchTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInput_ReturnsSuccessWithPendingStatus()
    {
        var result = CostImportBatch.Create("AWS CUR", "2026-03", FixedNow, "USD");

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().Be("AWS CUR");
        result.Value.Period.Should().Be("2026-03");
        result.Value.Currency.Should().Be("USD");
        result.Value.Status.Should().Be(CostImportBatch.StatusPending);
        result.Value.RecordCount.Should().Be(0);
        result.Value.ImportedAt.Should().Be(FixedNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySource_ThrowsArgumentException(string? source)
    {
        var act = () => CostImportBatch.Create(source!, "2026-03", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_SetsCompletedStatusAndRecordCount()
    {
        var batch = CostImportBatch.Create("AWS CUR", "2026-03", FixedNow).Value;

        batch.Complete(42);

        batch.Status.Should().Be(CostImportBatch.StatusCompleted);
        batch.RecordCount.Should().Be(42);
    }

    [Fact]
    public void Fail_SetsFailedStatusAndError()
    {
        var batch = CostImportBatch.Create("AWS CUR", "2026-03", FixedNow).Value;

        batch.Fail("Connection timeout");

        batch.Status.Should().Be(CostImportBatch.StatusFailed);
        batch.Error.Should().Be("Connection timeout");
    }
}

/// <summary>Testes unitários da entidade CostRecord — criação e validação de invariantes.</summary>
public sealed class CostRecordTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ValidBatchId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var result = CostRecord.Create(
            ValidBatchId, "svc-order", "Order Service", "team-platform", "commerce",
            "prod", "2026-03", 150.50m, "USD", "AWS CUR", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-order");
        result.Value.ServiceName.Should().Be("Order Service");
        result.Value.TotalCost.Should().Be(150.50m);
        result.Value.Period.Should().Be("2026-03");
        result.Value.Currency.Should().Be("USD");
        result.Value.Source.Should().Be("AWS CUR");
        result.Value.Team.Should().Be("team-platform");
        result.Value.Domain.Should().Be("commerce");
        result.Value.Environment.Should().Be("prod");
        result.Value.BatchId.Should().Be(ValidBatchId);
    }

    [Fact]
    public void Create_NegativeCost_ReturnsFailure()
    {
        var result = CostRecord.Create(
            ValidBatchId, "svc-order", "Order Service", null, null,
            null, "2026-03", -10m, "USD", "AWS CUR", FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Cost.Negative");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyServiceId_ThrowsArgumentException(string? serviceId)
    {
        var act = () => CostRecord.Create(
            ValidBatchId, serviceId!, "Order Service", null, null,
            null, "2026-03", 100m, "USD", "AWS CUR", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPeriod_ThrowsArgumentException(string? period)
    {
        var act = () => CostRecord.Create(
            ValidBatchId, "svc-order", "Order Service", null, null,
            null, period!, 100m, "USD", "AWS CUR", FixedNow);

        act.Should().Throw<ArgumentException>();
    }
}

/// <summary>Testes unitários do handler ImportCostBatch — fluxo completo com dependências mockadas.</summary>
public sealed class ImportCostBatchHandlerTests
{
    private readonly ICostImportBatchRepository _batchRepository = Substitute.For<ICostImportBatchRepository>();
    private readonly ICostRecordRepository _recordRepository = Substitute.For<ICostRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ImportCostBatch.Handler> _logger = Substitute.For<ILogger<ImportCostBatch.Handler>>();

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private ImportCostBatch.Handler CreateHandler() =>
        new(_batchRepository, _recordRepository, _unitOfWork, _clock, _logger);

    private static ImportCostBatch.Command CreateValidCommand(int recordCount = 2) =>
        new("AWS CUR", "2026-03", "USD",
            Enumerable.Range(1, recordCount)
                .Select(i => new ImportCostBatch.CostRecordInput(
                    $"svc-{i}", $"Service {i}", "team-platform", "commerce", "prod", 100m * i))
                .ToList());

    public ImportCostBatchHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _batchRepository.ExistsBySourceAndPeriodAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidBatch_CreatesBatchAndRecords()
    {
        var command = CreateValidCommand(3);
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().Be("AWS CUR");
        result.Value.Period.Should().Be("2026-03");
        result.Value.Currency.Should().Be("USD");
        result.Value.RecordCount.Should().Be(3);

        _batchRepository.Received(1).Add(Arg.Any<CostImportBatch>());
        _recordRepository.Received(1).AddRange(Arg.Is<IEnumerable<CostRecord>>(r => r.ToList().Count == 3));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyRecords_ReturnsError()
    {
        var command = new ImportCostBatch.Command("AWS CUR", "2026-03", "USD", []);
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CostImportBatch.Empty");
    }

    [Fact]
    public async Task Handle_ValidBatch_SetsBatchCompleted()
    {
        var command = CreateValidCommand(2);
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CostImportBatch.StatusCompleted);
        result.Value.RecordCount.Should().Be(2);
        result.Value.ImportedAt.Should().Be(FixedNow);
    }
}
