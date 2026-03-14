using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiOrchestration.Domain.Enums;
using NexTraceOne.AiOrchestration.Domain.Errors;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Representa cenários de teste ou scripts gerados por IA para validação de uma mudança ou release.
/// Cada artefato está vinculado a uma release, possui um framework de teste alvo e passa por
/// revisão humana antes de ser aceito para uso em pipelines de CI/CD.
///
/// Ciclo de vida: Draft → (Accepted | Rejected).
///
/// Invariantes:
/// - Confiança da IA deve estar no intervalo [0, 1].
/// - Artefato inicia em status Draft, aguardando revisão.
/// - Aceitar ou rejeitar requer identificação do revisor.
/// - Revisão só pode ser realizada uma vez.
/// </summary>
public sealed class GeneratedTestArtifact : AuditableEntity<GeneratedTestArtifactId>
{
    private GeneratedTestArtifact() { }

    /// <summary>Identificador da release para a qual os testes foram gerados.</summary>
    public Guid ReleaseId { get; private set; }

    /// <summary>Nome do serviço alvo dos testes gerados.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Framework de teste alvo (ex: "xunit", "nunit", "robot-framework").</summary>
    public string TestFramework { get; private set; } = string.Empty;

    /// <summary>Código-fonte dos testes gerados pela IA.</summary>
    public string GeneratedCode { get; private set; } = string.Empty;

    /// <summary>Nível de confiança da IA na qualidade dos testes, no intervalo [0, 1].</summary>
    public decimal Confidence { get; private set; }

    /// <summary>Estado atual de revisão do artefato.</summary>
    public ArtifactStatus Status { get; private set; } = ArtifactStatus.Draft;

    /// <summary>Identificador do revisor que aceitou ou rejeitou o artefato. Null se não revisado.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>Data/hora UTC da revisão. Null se ainda não revisado.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Data/hora UTC em que o artefato foi gerado pela IA.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>
    /// Gera um novo artefato de teste com validações de invariantes.
    /// O artefato inicia em status Draft, aguardando revisão humana.
    /// </summary>
    public static Result<GeneratedTestArtifact> Generate(
        Guid releaseId,
        string serviceName,
        string testFramework,
        string generatedCode,
        decimal confidence,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Default(releaseId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(testFramework);
        Guard.Against.NullOrWhiteSpace(generatedCode);

        if (confidence < 0m || confidence > 1m)
            return AiOrchestrationErrors.InvalidConfidence(confidence);

        return new GeneratedTestArtifact
        {
            Id = GeneratedTestArtifactId.New(),
            ReleaseId = releaseId,
            ServiceName = serviceName,
            TestFramework = testFramework,
            GeneratedCode = generatedCode,
            Confidence = confidence,
            GeneratedAt = generatedAt,
            Status = ArtifactStatus.Draft
        };
    }

    /// <summary>
    /// Aceita o artefato de teste após revisão humana.
    /// Torna o artefato disponível para integração em pipelines de CI/CD.
    /// Retorna erro se o artefato já foi revisado.
    /// </summary>
    public Result<Unit> Accept(string reviewedBy, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        if (Status != ArtifactStatus.Draft)
            return AiOrchestrationErrors.ArtifactAlreadyReviewed(Id.Value.ToString());

        Status = ArtifactStatus.Accepted;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Rejeita o artefato de teste após revisão humana.
    /// Retorna erro se o artefato já foi revisado.
    /// </summary>
    public Result<Unit> Reject(string reviewedBy, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        if (Status != ArtifactStatus.Draft)
            return AiOrchestrationErrors.ArtifactAlreadyReviewed(Id.Value.ToString());

        Status = ArtifactStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de GeneratedTestArtifact.</summary>
public sealed record GeneratedTestArtifactId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static GeneratedTestArtifactId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static GeneratedTestArtifactId From(Guid id) => new(id);
}
