using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="BackgroundServiceContractDetail"/>.
/// Valida criação, métodos de domínio e invariantes da entidade específica para jobs/workers/schedulers.
/// </summary>
public sealed class BackgroundServiceContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_Detail_For_CronJob()
    {
        var result = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "OrderExpirationJob",
            category: "Job",
            triggerType: "Cron",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: "[]",
            scheduleExpression: "0 * * * *");

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderExpirationJob");
        result.Value.Category.Should().Be("Job");
        result.Value.TriggerType.Should().Be("Cron");
        result.Value.ScheduleExpression.Should().Be("0 * * * *");
    }

    [Fact]
    public void Create_Should_Return_Valid_Detail_For_Worker()
    {
        var result = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "ReportGeneratorWorker",
            category: "Worker",
            triggerType: "Continuous",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: """["Writes to report_entries table"]""");

        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("Worker");
        result.Value.TriggerType.Should().Be("Continuous");
        result.Value.SideEffectsJson.Should().Contain("report_entries");
    }

    [Fact]
    public void Create_Should_Accept_AllowsConcurrency_True()
    {
        var result = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "ParallelIndexer",
            category: "Processor",
            triggerType: "Interval",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: "[]",
            allowsConcurrency: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllowsConcurrency.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Default_AllowsConcurrency_To_False()
    {
        var result = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "SingleInstanceJob",
            category: "Job",
            triggerType: "Cron",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: "[]");

        result.IsSuccess.Should().BeTrue();
        result.Value.AllowsConcurrency.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_Accept_TimeoutExpression()
    {
        var result = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "TimedJob",
            category: "Job",
            triggerType: "OnDemand",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: "[]",
            timeoutExpression: "PT30M");

        result.IsSuccess.Should().BeTrue();
        result.Value.TimeoutExpression.Should().Be("PT30M");
    }

    [Fact]
    public void Update_Should_Change_All_Fields()
    {
        var detail = BackgroundServiceContractDetail.Create(
            ValidVersionId,
            serviceName: "OldJob",
            category: "Job",
            triggerType: "OnDemand",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: "[]").Value;

        detail.Update(
            serviceName: "NewWorker",
            category: "Worker",
            triggerType: "Cron",
            inputsJson: """{"batchSize":"int"}""",
            outputsJson: """{"processedCount":"int"}""",
            sideEffectsJson: """["Updates orders table"]""",
            scheduleExpression: "0 2 * * *",
            timeoutExpression: "PT1H",
            allowsConcurrency: true);

        detail.ServiceName.Should().Be("NewWorker");
        detail.Category.Should().Be("Worker");
        detail.TriggerType.Should().Be("Cron");
        detail.ScheduleExpression.Should().Be("0 2 * * *");
        detail.TimeoutExpression.Should().Be("PT1H");
        detail.AllowsConcurrency.Should().BeTrue();
    }

    [Fact]
    public void BackgroundServiceContractDetailId_New_Should_Return_UniqueId()
    {
        var id1 = BackgroundServiceContractDetailId.New();
        var id2 = BackgroundServiceContractDetailId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void BackgroundServiceContractDetailId_From_Should_Roundtrip()
    {
        var guid = Guid.NewGuid();
        var id = BackgroundServiceContractDetailId.From(guid);

        id.Value.Should().Be(guid);
    }
}
