using System.Linq;

using NexTraceOne.Catalog.Application.Contracts.Generation;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts;

/// <summary>
/// Testes para AQ.4 — gerador de scaffold de aplicação por tipo de serviço (determinístico).
/// Garante que cada modelo (Kafka, SOAP, worker, REST, …) produz o esqueleto adequado.
/// </summary>
public sealed class ServiceScaffoldGeneratorTests
{
    [Fact]
    public void KafkaConsumer_GeneratesBackgroundServiceConsumer()
    {
        var files = ServiceScaffoldGenerator.Generate("payment-events", ServiceType.KafkaConsumer);

        var consumer = files.Single(f => f.Path == "src/payment-events/Consumers/PaymentEventsConsumer.cs");
        consumer.Content.Should().Contain("public sealed class PaymentEventsConsumer : BackgroundService");
        consumer.Content.Should().Contain("ExecuteAsync(CancellationToken stoppingToken)");
        files.Should().Contain(f => f.Path.EndsWith("README.md"));
    }

    [Fact]
    public void KafkaProducer_GeneratesProducerWithPublishAsync()
    {
        var files = ServiceScaffoldGenerator.Generate("payment-events", ServiceType.KafkaProducer);

        var producer = files.Single(f => f.Path.EndsWith("PaymentEventsProducer.cs"));
        producer.Content.Should().Contain("public sealed class PaymentEventsProducer");
        producer.Content.Should().Contain("PublishAsync(object message");
    }

    [Fact]
    public void SoapService_GeneratesServiceInterfaceAndImplementation()
    {
        var files = ServiceScaffoldGenerator.Generate("billing-soap", ServiceType.SoapService);

        var svc = files.Single(f => f.Path.EndsWith("BillingSoapService.cs"));
        svc.Content.Should().Contain("public interface IBillingSoapService");
        svc.Content.Should().Contain("public sealed class BillingSoapService : IBillingSoapService");
    }

    [Theory]
    [InlineData(ServiceType.BackgroundService)]
    [InlineData(ServiceType.ScheduledProcess)]
    [InlineData(ServiceType.BatchJob)]
    public void WorkerTypes_GenerateBackgroundServiceWorker(ServiceType serviceType)
    {
        var files = ServiceScaffoldGenerator.Generate("nightly-job", serviceType);

        var worker = files.Single(f => f.Path.EndsWith("NightlyJobWorker.cs"));
        worker.Content.Should().Contain("public sealed class NightlyJobWorker : BackgroundService");
    }

    [Fact]
    public void RestApi_WithoutContract_GeneratesMinimalApiProgram()
    {
        var files = ServiceScaffoldGenerator.Generate("orders-api", ServiceType.RestApi);

        var program = files.Single(f => f.Path == "src/orders-api.Api/Program.cs");
        program.Content.Should().Contain("WebApplication.CreateBuilder(args)");
        program.Content.Should().Contain("app.MapGet(\"/health\"");
    }

    [Fact]
    public void UnknownModel_GeneratesReadmeOnly()
    {
        var files = ServiceScaffoldGenerator.Generate("legacy-thing", ServiceType.ThirdParty);

        files.Should().ContainSingle();
        files[0].Path.Should().EndWith("README.md");
        files[0].Content.Should().Contain("ThirdParty");
    }
}
