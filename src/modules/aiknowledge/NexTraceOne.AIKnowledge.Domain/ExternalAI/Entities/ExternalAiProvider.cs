using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ExternalAi.Domain.Errors;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Representa um provedor externo de IA registrado na plataforma (ex: OpenAI, Azure OpenAI, Claude).
/// Cada provedor possui configuração de endpoint, modelo, limites de tokens, custo e prioridade
/// de roteamento para seleção automática durante consultas.
///
/// Invariantes:
/// - Prioridade menor indica preferência maior no roteamento.
/// - MaxTokensPerRequest e CostPerToken devem ser positivos.
/// - Ativação e desativação são idempotentes.
/// </summary>
public sealed class ExternalAiProvider : AuditableEntity<ExternalAiProviderId>
{
    private ExternalAiProvider() { }

    /// <summary>Nome identificador do provedor (ex: "OpenAI GPT-4", "Azure OpenAI").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL do endpoint da API do provedor.</summary>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>Nome do modelo utilizado (ex: "gpt-4", "gpt-3.5-turbo", "claude-3-opus").</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Indica se o provedor está ativo e disponível para roteamento de consultas.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Limite máximo de tokens por requisição individual.</summary>
    public int MaxTokensPerRequest { get; private set; }

    /// <summary>Custo por token consumido, utilizado para estimativa de custos.</summary>
    public decimal CostPerToken { get; private set; }

    /// <summary>Prioridade de roteamento — valor menor indica maior preferência.</summary>
    public int Priority { get; private set; }

    /// <summary>Data/hora UTC em que o provedor foi registrado na plataforma.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Registra um novo provedor de IA com validações de invariantes.
    /// O provedor inicia ativo e disponível para roteamento.
    /// </summary>
    public static ExternalAiProvider Register(
        string name,
        string endpoint,
        string modelName,
        int maxTokensPerRequest,
        decimal costPerToken,
        int priority,
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(endpoint);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.NegativeOrZero(maxTokensPerRequest);
        Guard.Against.Negative(costPerToken);
        Guard.Against.Negative(priority);

        return new ExternalAiProvider
        {
            Id = ExternalAiProviderId.New(),
            Name = name,
            Endpoint = endpoint,
            ModelName = modelName,
            IsActive = true,
            MaxTokensPerRequest = maxTokensPerRequest,
            CostPerToken = costPerToken,
            Priority = priority,
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Desativa o provedor, removendo-o do pool de roteamento de consultas.
    /// Operação idempotente — não retorna erro se já desativado.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>
    /// Reativa o provedor, tornando-o disponível para roteamento de consultas.
    /// Operação idempotente — não retorna erro se já ativo.
    /// </summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza a prioridade de roteamento do provedor.
    /// Prioridade menor indica maior preferência na seleção automática.
    /// </summary>
    public Result<Unit> UpdatePriority(int newPriority)
    {
        Guard.Against.Negative(newPriority);

        Priority = newPriority;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ExternalAiProvider.</summary>
public sealed record ExternalAiProviderId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiProviderId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiProviderId From(Guid id) => new(id);
}
