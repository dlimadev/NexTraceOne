namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

/// <summary>
/// Payload unificado de alerta operacional da plataforma.
/// Contém título, descrição, severidade, origem, correlação e contexto adicional.
/// Imutável por design (record) para segurança em fan-out para múltiplos canais.
/// </summary>
public sealed record AlertPayload
{
    /// <summary>Título curto e descritivo do alerta.</summary>
    public required string Title { get; init; }

    /// <summary>Descrição detalhada do alerta, com contexto operacional.</summary>
    public required string Description { get; init; }

    /// <summary>Severidade do alerta.</summary>
    public required AlertSeverity Severity { get; init; }

    /// <summary>Origem do alerta (serviço, módulo ou componente).</summary>
    public required string Source { get; init; }

    /// <summary>Identificador de correlação para rastreabilidade cross-system.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Timestamp UTC do alerta.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Contexto adicional (chave-valor) para enriquecer o alerta.</summary>
    public Dictionary<string, string> Context { get; init; } = new();
}
