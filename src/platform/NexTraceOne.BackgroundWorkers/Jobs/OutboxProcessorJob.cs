namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job Quartz.NET que processa mensagens pendentes na tabela Outbox.
/// Executa a cada 5 segundos, lê batch de mensagens não processadas,
/// deserializa e entrega aos handlers de Integration Event correspondentes.
/// Garante at-least-once delivery com retry exponencial.
/// </summary>
public sealed class OutboxProcessorJob
{
    // TODO: Implementar IJob com lógica de processamento do Outbox
}
