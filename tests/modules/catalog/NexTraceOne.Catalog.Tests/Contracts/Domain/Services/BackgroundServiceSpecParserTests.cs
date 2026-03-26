using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes do BackgroundServiceSpecParser.
/// Valida o parsing de specs JSON de Background Service Contracts,
/// incluindo cenários de conteúdo válido, parcial, vazio e malformado.
/// </summary>
public sealed class BackgroundServiceSpecParserTests
{
    [Fact]
    public void Parse_Should_ReturnFullSpec_When_JsonIsComplete()
    {
        const string json = """
        {
            "serviceName": "OrderExpirationJob",
            "category": "Job",
            "triggerType": "Cron",
            "scheduleExpression": "0 * * * *",
            "timeoutExpression": "PT30M",
            "allowsConcurrency": false,
            "inputs": { "orderId": "Guid|The order to expire" },
            "outputs": { "expiredCount": "int|Number of orders expired" },
            "sideEffects": ["Writes to order_history", "Publishes OrderExpired event"]
        }
        """;

        var spec = BackgroundServiceSpecParser.Parse(json);

        spec.ServiceName.Should().Be("OrderExpirationJob");
        spec.Category.Should().Be("Job");
        spec.TriggerType.Should().Be("Cron");
        spec.ScheduleExpression.Should().Be("0 * * * *");
        spec.TimeoutExpression.Should().Be("PT30M");
        spec.AllowsConcurrency.Should().BeFalse();
        spec.Inputs.Should().ContainKey("orderId");
        spec.Outputs.Should().ContainKey("expiredCount");
        spec.SideEffects.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_Should_ReturnDefaultTrigger_When_TriggerTypeMissing()
    {
        const string json = """{ "serviceName": "MyWorker", "category": "Worker" }""";

        var spec = BackgroundServiceSpecParser.Parse(json);

        spec.ServiceName.Should().Be("MyWorker");
        spec.TriggerType.Should().Be("OnDemand");
        spec.Inputs.Should().BeEmpty();
        spec.Outputs.Should().BeEmpty();
        spec.SideEffects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_ReturnEmptySpec_When_ContentIsEmpty()
    {
        var spec = BackgroundServiceSpecParser.Parse(string.Empty);

        spec.ServiceName.Should().BeEmpty();
        spec.TriggerType.Should().Be("OnDemand");
        spec.Inputs.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_ReturnEmptySpec_When_ContentIsEmptyObject()
    {
        var spec = BackgroundServiceSpecParser.Parse("{}");

        spec.ServiceName.Should().BeEmpty();
        spec.Inputs.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_ReturnEmptySpec_When_ContentIsMalformed()
    {
        var spec = BackgroundServiceSpecParser.Parse("{ invalid json {{");

        spec.ServiceName.Should().BeEmpty();
        spec.TriggerType.Should().Be("OnDemand");
    }

    [Fact]
    public void Parse_Should_ParseAllowsConcurrencyTrue_When_Provided()
    {
        const string json = """{ "serviceName": "ParallelWorker", "allowsConcurrency": true }""";

        var spec = BackgroundServiceSpecParser.Parse(json);

        spec.AllowsConcurrency.Should().BeTrue();
    }
}
