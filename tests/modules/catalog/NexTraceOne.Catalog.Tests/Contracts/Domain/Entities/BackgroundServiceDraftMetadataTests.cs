using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="BackgroundServiceDraftMetadata"/>.
/// Valida criação e atualização dos metadados específicos de background service para drafts.
/// </summary>
public sealed class BackgroundServiceDraftMetadataTests
{
    private static readonly ContractDraftId ValidDraftId = ContractDraftId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_Metadata_With_Defaults()
    {
        var metadata = BackgroundServiceDraftMetadata.Create(
            ValidDraftId,
            serviceName: "PaymentProcessorJob");

        metadata.ServiceName.Should().Be("PaymentProcessorJob");
        metadata.Category.Should().Be("Job");
        metadata.TriggerType.Should().Be("OnDemand");
        metadata.ScheduleExpression.Should().BeNull();
        metadata.InputsJson.Should().Be("{}");
        metadata.OutputsJson.Should().Be("{}");
        metadata.SideEffectsJson.Should().Be("[]");
    }

    [Fact]
    public void Create_Should_Accept_Cron_Category_And_Schedule()
    {
        var metadata = BackgroundServiceDraftMetadata.Create(
            ValidDraftId,
            serviceName: "NightlyReportJob",
            category: "Scheduler",
            triggerType: "Cron",
            scheduleExpression: "0 2 * * *");

        metadata.Category.Should().Be("Scheduler");
        metadata.TriggerType.Should().Be("Cron");
        metadata.ScheduleExpression.Should().Be("0 2 * * *");
    }

    [Fact]
    public void Create_Should_Accept_Initial_InputsOutputsAndSideEffects()
    {
        var metadata = BackgroundServiceDraftMetadata.Create(
            ValidDraftId,
            serviceName: "DataExporter",
            inputsJson: """{"startDate":"datetime"}""",
            outputsJson: """{"exportedRows":"int"}""",
            sideEffectsJson: """["Writes CSV to blob storage"]""");

        metadata.InputsJson.Should().Contain("startDate");
        metadata.OutputsJson.Should().Contain("exportedRows");
        metadata.SideEffectsJson.Should().Contain("blob storage");
    }

    [Fact]
    public void Update_Should_Change_All_Fields()
    {
        var metadata = BackgroundServiceDraftMetadata.Create(
            ValidDraftId,
            serviceName: "OldJob");

        metadata.Update(
            serviceName: "UpdatedWorker",
            category: "Worker",
            triggerType: "Continuous",
            scheduleExpression: null,
            inputsJson: """{"topic":"string"}""",
            outputsJson: """{"processedCount":"int"}""",
            sideEffectsJson: """["Publishes to notification.sent"]""");

        metadata.ServiceName.Should().Be("UpdatedWorker");
        metadata.Category.Should().Be("Worker");
        metadata.TriggerType.Should().Be("Continuous");
        metadata.ScheduleExpression.Should().BeNull();
        metadata.InputsJson.Should().Contain("topic");
        metadata.SideEffectsJson.Should().Contain("notification.sent");
    }

    [Fact]
    public void BackgroundServiceDraftMetadataId_New_Should_Return_UniqueId()
    {
        var id1 = BackgroundServiceDraftMetadataId.New();
        var id2 = BackgroundServiceDraftMetadataId.New();

        id1.Should().NotBe(id2);
    }
}
