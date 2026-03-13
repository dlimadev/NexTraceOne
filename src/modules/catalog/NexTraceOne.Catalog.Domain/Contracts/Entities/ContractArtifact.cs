using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Entidade que representa um artefato gerado a partir de uma versão de contrato.
/// Inclui testes de conformidade, scaffolds de serviço, documentação, changelogs,
/// guias de migração e evidências regulatórias. Cada artefato tem proveniência
/// clara (gerado manualmente, por automação ou por IA local) e é vinculado à
/// versão específica do contrato que o originou.
/// </summary>
public sealed class ContractArtifact : Entity<ContractArtifactId>
{
    private ContractArtifact() { }

    /// <summary>Identificador da versão de contrato que originou o artefato.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = null!;

    /// <summary>Tipo do artefato gerado (teste, scaffold, documentação, etc.).</summary>
    public ContractArtifactType ArtifactType { get; private set; }

    /// <summary>Nome descritivo do artefato (ex: "user-service-conformance-tests.cs").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Conteúdo do artefato gerado (código, JSON, XML, Markdown, etc.).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Formato do conteúdo (ex: "csharp", "json", "markdown", "yaml").</summary>
    public string ContentFormat { get; private set; } = string.Empty;

    /// <summary>Indica se o artefato foi gerado por IA local.</summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>Timestamp de quando o artefato foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Usuário que solicitou a geração do artefato.</summary>
    public string GeneratedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo artefato gerado a partir de uma versão de contrato.
    /// </summary>
    public static ContractArtifact Create(
        ContractVersionId contractVersionId,
        ContractArtifactType artifactType,
        string name,
        string content,
        string contentFormat,
        string generatedBy,
        DateTimeOffset generatedAt,
        bool isAiGenerated = false)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(contentFormat);
        Guard.Against.NullOrWhiteSpace(generatedBy);

        return new ContractArtifact
        {
            Id = ContractArtifactId.New(),
            ContractVersionId = contractVersionId,
            ArtifactType = artifactType,
            Name = name,
            Content = content,
            ContentFormat = contentFormat,
            IsAiGenerated = isAiGenerated,
            GeneratedAt = generatedAt,
            GeneratedBy = generatedBy
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractArtifact.</summary>
public sealed record ContractArtifactId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractArtifactId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractArtifactId From(Guid id) => new(id);
}
