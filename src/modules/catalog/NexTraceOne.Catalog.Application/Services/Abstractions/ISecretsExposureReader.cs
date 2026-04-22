namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstração de leitura de textos de artefactos governados para varredura de segredos expostos.
///
/// Fornece o conteúdo textual de contratos, notas operacionais e runbooks de um tenant
/// para que o handler de exposição de segredos possa aplicar pattern matching sem depender
/// directamente de múltiplos repositórios.
/// Desacopla o handler de secrets exposure das implementações concretas de repositório.
///
/// Wave AD.2 — GetSecretsExposureRiskReport.
/// </summary>
public interface ISecretsExposureReader
{
    /// <summary>
    /// Lista textos de artefactos governados de um tenant para varredura de segredos.
    /// </summary>
    Task<IReadOnlyList<ArtifactTextEntry>> ListArtifactTextsAsync(
        string tenantId,
        int maxArtifacts,
        CancellationToken ct);
}

/// <summary>
/// Entrada de texto de um artefacto governado para varredura de segredos expostos.
/// Wave AD.2.
/// </summary>
public sealed record ArtifactTextEntry(
    /// <summary>Identificador único do artefacto.</summary>
    string ArtifactId,
    /// <summary>Tipo do artefacto: Contract, OperationalNote ou Runbook.</summary>
    string ArtifactType,
    /// <summary>Nome do serviço associado ao artefacto.</summary>
    string ServiceName,
    /// <summary>Título ou nome do artefacto.</summary>
    string Title,
    /// <summary>Conteúdo textual do artefacto a varrer.</summary>
    string Content);
