using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

/// <summary>
/// Representa uma política de governança que controla o uso de IA externa na plataforma.
/// Define limites diários de consultas e tokens, requer aprovação para determinados
/// contextos e restringe quais tipos de análise podem utilizar IA externa.
///
/// Invariantes:
/// - Limites de consultas e tokens devem ser positivos.
/// - AllowedContexts é uma lista separada por vírgula de contextos permitidos.
/// - Política inativa não é avaliada durante o roteamento de consultas.
/// </summary>
public sealed class ExternalAiPolicy : AuditableEntity<ExternalAiPolicyId>
{
    private ExternalAiPolicy() { }

    /// <summary>Nome identificador da política de governança.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do propósito e escopo da política.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Limite máximo de consultas permitidas por dia.</summary>
    public int MaxDailyQueries { get; private set; }

    /// <summary>Limite máximo de tokens consumidos por dia em todas as consultas.</summary>
    public long MaxTokensPerDay { get; private set; }

    /// <summary>Indica se consultas neste contexto requerem aprovação prévia.</summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>Lista de contextos permitidos separados por vírgula (ex: "change-analysis,error-diagnosis").</summary>
    public string AllowedContexts { get; private set; } = string.Empty;

    /// <summary>Indica se a política está ativa e sendo avaliada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria uma nova política de governança de IA com validações de invariantes.
    /// A política inicia ativa e pronta para avaliação.
    /// Os campos de auditoria (CreatedAt, CreatedBy) são preenchidos automaticamente
    /// pelo AuditInterceptor do DbContext — não é responsabilidade do domínio.
    /// </summary>
    public static ExternalAiPolicy Create(
        string name,
        string description,
        int maxDailyQueries,
        long maxTokensPerDay,
        bool requiresApproval,
        string allowedContexts,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NegativeOrZero(maxDailyQueries);
        Guard.Against.NegativeOrZero(maxTokensPerDay);
        Guard.Against.NullOrWhiteSpace(allowedContexts);

        return new ExternalAiPolicy
        {
            Id = ExternalAiPolicyId.New(),
            Name = name,
            Description = description,
            MaxDailyQueries = maxDailyQueries,
            MaxTokensPerDay = maxTokensPerDay,
            RequiresApproval = requiresApproval,
            AllowedContexts = allowedContexts,
            IsActive = true
        };
    }

    /// <summary>
    /// Atualiza os parâmetros da política de governança.
    /// Permite ajustar limites, requisitos de aprovação e contextos permitidos.
    /// </summary>
    public Result<Unit> Update(
        string description,
        int maxDailyQueries,
        long maxTokensPerDay,
        bool requiresApproval,
        string allowedContexts)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NegativeOrZero(maxDailyQueries);
        Guard.Against.NegativeOrZero(maxTokensPerDay);
        Guard.Against.NullOrWhiteSpace(allowedContexts);

        Description = description;
        MaxDailyQueries = maxDailyQueries;
        MaxTokensPerDay = maxTokensPerDay;
        RequiresApproval = requiresApproval;
        AllowedContexts = allowedContexts;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a política, removendo-a da avaliação durante roteamento de consultas.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se um contexto específico é permitido por esta política.
    /// A verificação é case-insensitive e considera a lista separada por vírgula.
    /// </summary>
    public bool IsContextAllowed(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return false;

        var contexts = AllowedContexts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return contexts.Any(c => c.Equals(context, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Identificador fortemente tipado de ExternalAiPolicy.</summary>
public sealed record ExternalAiPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiPolicyId From(Guid id) => new(id);
}
