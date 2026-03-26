using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes do CanonicalModelBuilder para o protocolo WorkerService.
/// Valida que o modelo canónico é construído corretamente a partir de specs de Background Service.
/// </summary>
public sealed class CanonicalModelBuilderWorkerServiceTests
{
    private const string FullSpec = """
    {
        "serviceName": "OrderExpirationJob",
        "category": "Job",
        "triggerType": "Cron",
        "scheduleExpression": "0 * * * *",
        "timeoutExpression": "PT30M",
        "allowsConcurrency": false,
        "inputs": { "orderId": "Guid", "tenantId": "Guid" },
        "outputs": { "expiredCount": "int", "errorCount": "int" },
        "sideEffects": ["Writes to order_history"]
    }
    """;

    [Fact]
    public void Build_Should_ReturnWorkerServiceModel_When_ProtocolIsWorkerService()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.Protocol.Should().Be(ContractProtocol.WorkerService);
        model.Title.Should().Be("OrderExpirationJob");
        model.SpecVersion.Should().Be("Cron");
    }

    [Fact]
    public void Build_Should_MapInputsAsOperationParameters()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.OperationCount.Should().Be(1);
        var op = model.Operations.First();
        op.OperationId.Should().Be("OrderExpirationJob");
        op.InputParameters.Should().HaveCount(2);
        op.InputParameters.Should().Contain(p => p.Name == "orderId");
    }

    [Fact]
    public void Build_Should_MapOutputsAsSchemas()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.SchemaCount.Should().Be(2);
        model.GlobalSchemas.Should().Contain(s => s.Name == "expiredCount");
    }

    [Fact]
    public void Build_Should_SetHasSecurityDefinitionsFalse_ForWorkerService()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.HasSecurityDefinitions.Should().BeFalse();
        model.SecuritySchemes.Should().BeEmpty();
    }

    [Fact]
    public void Build_Should_SetHasExamplesTrue_When_InputsOrOutputsPresent()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.HasExamples.Should().BeTrue();
    }

    [Fact]
    public void Build_Should_SetCategoryAsTag()
    {
        var model = CanonicalModelBuilder.Build(FullSpec, ContractProtocol.WorkerService);

        model.Tags.Should().Contain("Job");
    }

    [Fact]
    public void Build_Should_ReturnEmptyModel_When_SpecContentIsEmpty()
    {
        var model = CanonicalModelBuilder.Build("{}", ContractProtocol.WorkerService);

        model.Protocol.Should().Be(ContractProtocol.WorkerService);
        model.Title.Should().Be("Unknown Worker");
        model.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Build_Should_ReturnEmptyModel_When_SpecIsMalformed()
    {
        var model = CanonicalModelBuilder.Build("{ invalid", ContractProtocol.WorkerService);

        model.Protocol.Should().Be(ContractProtocol.WorkerService);
        model.OperationCount.Should().Be(0);
    }
}

/// <summary>
/// Testes de routing do ContractDiffCalculator para o protocolo WorkerService.
/// Garante que WorkerService é roteado para WorkerServiceDiffCalculator.
/// </summary>
public sealed class ContractDiffCalculatorWorkerServiceRoutingTests
{
    private const string CronSpec = """
    {
        "serviceName": "ReportGenerator",
        "triggerType": "Cron",
        "scheduleExpression": "0 2 * * *",
        "inputs": { "reportType": "string" },
        "outputs": { "fileUrl": "string" }
    }
    """;

    [Fact]
    public void ComputeDiff_Should_UseWorkerServiceCalculator_ForWorkerServiceProtocol()
    {
        // Changing schedule expression should be breaking
        const string targetSpec = """
        {
            "serviceName": "ReportGenerator",
            "triggerType": "Cron",
            "scheduleExpression": "0 4 * * *",
            "inputs": { "reportType": "string" },
            "outputs": { "fileUrl": "string" }
        }
        """;

        var result = ContractDiffCalculator.ComputeDiff(CronSpec, targetSpec, ContractProtocol.WorkerService);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "ScheduleExpressionChanged");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnEmptyResult_ForProtobufProtocol()
    {
        var result = ContractDiffCalculator.ComputeDiff("{}", "{}", ContractProtocol.Protobuf);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
    }
}
