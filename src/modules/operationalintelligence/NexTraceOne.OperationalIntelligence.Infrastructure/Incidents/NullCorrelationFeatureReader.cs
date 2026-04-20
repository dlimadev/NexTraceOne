using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação nula (null object) de ICorrelationFeatureReader.
/// Utilizada enquanto a integração com o módulo ChangeGovernance não está completa.
/// Retorna sempre null — o handler de ScoreCorrelationFeatureSet trata este caso
/// com uma resposta com score zero e explicação adequada.
/// </summary>
internal sealed class NullCorrelationFeatureReader : ICorrelationFeatureReader
{
    /// <inheritdoc />
    public Task<ChangeReleaseDetails?> GetChangeDetailsAsync(Guid changeId, CancellationToken ct)
        => Task.FromResult<ChangeReleaseDetails?>(null);
}
