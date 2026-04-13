using Ardalis.GuardClauses;

using MediatR;

using System.Text.Json;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma fonte de conhecimento configurada para grounding/contexto das consultas de IA.
/// Cada fonte mapeia um domínio do NexTraceOne (serviços, contratos, incidentes, etc.)
/// como contexto disponível para enriquecimento das respostas da IA.
///
/// Invariantes:
/// - Nome e tipo de fonte são obrigatórios.
/// - Prioridade deve ser não-negativa (menor = maior prioridade).
/// - Fonte inicia sempre ativa.
/// </summary>
public sealed class AIKnowledgeSource : AuditableEntity<AIKnowledgeSourceId>
{
    private AIKnowledgeSource() { }

    /// <summary>Nome identificador da fonte de conhecimento.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do conteúdo e propósito da fonte.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo de fonte de conhecimento — mapeia para um domínio do NexTraceOne.</summary>
    public KnowledgeSourceType SourceType { get; private set; }

    /// <summary>Endpoint ou caminho de acesso à fonte de dados.</summary>
    public string EndpointOrPath { get; private set; } = string.Empty;

    /// <summary>Indica se a fonte está ativa e disponível para consultas de IA.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Prioridade de utilização — valor menor indica maior prioridade.</summary>
    public int Priority { get; private set; }

    /// <summary>Data/hora UTC em que a fonte foi registrada.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Representação JSON do vetor de embedding da fonte, gerado pelo job de indexação.
    /// Null quando ainda não indexado.
    /// </summary>
    public string? EmbeddingJson { get; private set; }

    /// <summary>
    /// Regista uma nova fonte de conhecimento com validações de invariantes.
    /// A fonte inicia ativa e disponível para consultas de IA.
    /// </summary>
    public static AIKnowledgeSource Register(
        string name,
        string description,
        KnowledgeSourceType sourceType,
        string endpointOrPath,
        int priority,
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(endpointOrPath);
        Guard.Against.Negative(priority);

        return new AIKnowledgeSource
        {
            Id = AIKnowledgeSourceId.New(),
            Name = name,
            Description = description,
            SourceType = sourceType,
            EndpointOrPath = endpointOrPath,
            IsActive = true,
            Priority = priority,
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Ativa a fonte de conhecimento, tornando-a disponível para consultas de IA.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a fonte de conhecimento, removendo-a do pool de contexto disponível.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza a prioridade de utilização da fonte.
    /// Prioridade menor indica maior preferência na seleção de contexto.
    /// </summary>
    public Result<Unit> UpdatePriority(int newPriority)
    {
        Guard.Against.Negative(newPriority);

        Priority = newPriority;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza a descrição e caminho de acesso da fonte de conhecimento.
    /// </summary>
    public Result<Unit> Update(string description, string endpointOrPath)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(endpointOrPath);

        Description = description;
        EndpointOrPath = endpointOrPath;
        return Unit.Value;
    }

    /// <summary>
    /// Armazena o vetor de embedding serializado como JSON.
    /// Usado pelo EmbeddingIndexJob para persistir o resultado da indexação semântica.
    /// </summary>
    public void SetEmbedding(float[] embedding)
    {
        Guard.Against.Null(embedding);
        EmbeddingJson = JsonSerializer.Serialize(embedding);
    }

    /// <summary>
    /// Desserializa e retorna o vetor de embedding, ou null se não indexado.
    /// </summary>
    public float[]? GetEmbedding()
    {
        if (string.IsNullOrWhiteSpace(EmbeddingJson))
            return null;
        return JsonSerializer.Deserialize<float[]>(EmbeddingJson);
    }
}

/// <summary>Identificador fortemente tipado de AIKnowledgeSource.</summary>
public sealed record AIKnowledgeSourceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIKnowledgeSourceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIKnowledgeSourceId From(Guid id) => new(id);
}
