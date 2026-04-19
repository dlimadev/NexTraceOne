using Confluent.Kafka;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.Integrations.Infrastructure.Kafka;

/// <summary>
/// BackgroundService que consome mensagens de tópicos Kafka configurados.
/// Activa quando Kafka:Enabled = true e Kafka:BootstrapServers estão configurados.
/// Subscreve os tópicos listados em Kafka:Topics:Inbound (vírgula-separados).
/// </summary>
internal sealed class KafkaConsumerWorker(
    IConfiguration configuration,
    ILogger<KafkaConsumerWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        var enabled = configuration.GetValue<bool>("Kafka:Enabled");

        if (!enabled || string.IsNullOrWhiteSpace(bootstrapServers))
        {
            logger.LogDebug("KafkaConsumerWorker: Kafka not enabled or BootstrapServers not configured. Worker will not start.");
            return;
        }

        var inboundTopicsRaw = configuration["Kafka:Topics:Inbound"] ?? string.Empty;
        var topics = inboundTopicsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (topics.Length == 0)
        {
            logger.LogWarning("KafkaConsumerWorker: No inbound topics configured in Kafka:Topics:Inbound. Worker will not start.");
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "nextraceone-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        logger.LogInformation(
            "KafkaConsumerWorker: Starting. Subscribing to topics: {Topics}",
            string.Join(", ", topics));

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result is null)
                    {
                        continue;
                    }

                    var eventType = result.Message.Headers.TryGetLastBytes("event-type", out var headerBytes)
                        ? System.Text.Encoding.UTF8.GetString(headerBytes)
                        : "unknown";

                    logger.LogInformation(
                        "KafkaConsumerWorker: Received message. Topic={Topic} Partition={Partition} Offset={Offset} Key={Key} EventType={EventType}",
                        result.Topic, result.Partition, result.Offset, result.Message.Key, eventType);

                    // Processa a mensagem num scope de DI para serviços Scoped
                    using var scope = serviceScopeFactory.CreateScope();
                    // Ponto de extensão: injectar IKafkaMessageDispatcher quando implementado
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(
                        ex,
                        "KafkaConsumerWorker: Consume error on topic. Reason={Reason}",
                        ex.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "KafkaConsumerWorker: Unexpected error during consume loop.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("KafkaConsumerWorker: Consumer closed.");
        }
    }
}
