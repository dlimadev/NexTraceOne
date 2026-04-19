using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Integrations.Domain;
using NexTraceOne.Integrations.Infrastructure.Kafka;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes unitários para a lógica de registo DI do Kafka e NullKafkaEventProducer.
/// Os testes de comportamento do NullKafkaEventProducer estão em NullKafkaEventProducerTests.
/// </summary>
public sealed class ConfluentKafkaTests
{
    [Fact]
    public void NullKafkaEventProducer_IsConfigured_ReturnsFalse()
    {
        // Arrange — instanciar via interface para evitar referência directa ao tipo interno
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IKafkaEventProducer, NullKafkaEventProducer>();
        var provider = services.BuildServiceProvider();

        // Act
        var producer = provider.GetRequiredService<IKafkaEventProducer>();

        // Assert
        producer.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task NullKafkaEventProducer_ProduceAsync_Should_CompleteWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IKafkaEventProducer, NullKafkaEventProducer>();
        var provider = services.BuildServiceProvider();
        var producer = provider.GetRequiredService<IKafkaEventProducer>();

        var act = async () => await producer.ProduceAsync(
            "test.topic", "key-1", "TestEvent", """{"id":"1"}""", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NullKafkaEventProducer_ProduceBatchAsync_Should_CompleteWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IKafkaEventProducer, NullKafkaEventProducer>();
        var provider = services.BuildServiceProvider();
        var producer = provider.GetRequiredService<IKafkaEventProducer>();

        var messages = new List<KafkaMessage>
        {
            new("key-1", "EventA", """{"id":"1"}"""),
            new("key-2", "EventB", """{"id":"2"}""")
        };

        var act = async () => await producer.ProduceBatchAsync(
            "test.topic", messages, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void DI_WhenKafkaDisabled_ShouldRegister_NullKafkaEventProducer()
    {
        // Arrange — registo manual da mesma lógica condicional do DependencyInjection.cs
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Replica a lógica condicional de DependencyInjection.AddIntegrationsInfrastructure
        var kafkaEnabled = config.GetValue<bool>("Kafka:Enabled");
        var kafkaBootstrap = config["Kafka:BootstrapServers"];
        if (kafkaEnabled && !string.IsNullOrWhiteSpace(kafkaBootstrap))
        {
            services.AddSingleton<IKafkaEventProducer, ConfluentKafkaEventProducer>();
        }
        else
        {
            services.AddSingleton<IKafkaEventProducer, NullKafkaEventProducer>();
        }

        // Act
        var provider = services.BuildServiceProvider();
        var producer = provider.GetRequiredService<IKafkaEventProducer>();

        // Assert — quando Kafka está desactivo, deve ser NullKafkaEventProducer (IsConfigured=false)
        producer.IsConfigured.Should().BeFalse();
    }
}
