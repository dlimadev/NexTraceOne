namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

/// <summary>
/// Resultado do dispatch de um alerta para um ou mais canais.
/// Contém o resultado individual por canal e indicadores globais de sucesso.
/// </summary>
public sealed record AlertDispatchResult
{
    /// <summary>Resultado individual por canal (nome do canal → sucesso/falha).</summary>
    public IReadOnlyDictionary<string, bool> ChannelResults { get; init; }
        = new Dictionary<string, bool>();

    /// <summary>Indica se todos os canais processaram o alerta com sucesso.</summary>
    public bool AllSucceeded => ChannelResults.Count > 0
        && ChannelResults.Values.All(v => v);

    /// <summary>Indica se pelo menos um canal processou o alerta com sucesso.</summary>
    public bool AnySucceeded => ChannelResults.Values.Any(v => v);

    /// <summary>Número total de canais que receberam o alerta.</summary>
    public int TotalChannels => ChannelResults.Count;

    /// <summary>Número de canais que falharam no envio.</summary>
    public int FailedChannels => ChannelResults.Values.Count(v => !v);
}
