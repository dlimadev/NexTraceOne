using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação null (honest-null) de <see cref="ISecretsExposureReader"/>.
/// Retorna lista vazia — nenhum artefacto para varredura de segredos expostos.
/// Wave AD.2 — GetSecretsExposureRiskReport.
/// </summary>
public sealed class NullSecretsExposureReader : ISecretsExposureReader
{
    public Task<IReadOnlyList<ArtifactTextEntry>> ListArtifactTextsAsync(
        string tenantId,
        int maxArtifacts,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ArtifactTextEntry>>([]);
}
