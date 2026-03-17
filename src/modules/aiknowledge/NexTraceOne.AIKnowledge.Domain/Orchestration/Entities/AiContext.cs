using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

/// <summary>
/// Representa dados contextuais montados para uma consulta de IA.
/// Agrega informações como diffs de mudanças, blast radius, logs de erro e métricas
/// relevantes em um payload JSON estruturado para envio ao provedor de IA.
///
/// Invariantes:
/// - Payload não pode ser vazio — deve conter dados contextuais válidos.
/// - ContextType define o tipo de análise (change-analysis, error-diagnosis, test-generation).
/// - TokenEstimate é calculado a partir do tamanho do payload como estimativa de consumo.
/// </summary>
public sealed class AiContext : AuditableEntity<AiContextId>
{
    /// <summary>Fator de estimativa: ~4 caracteres por token (heurística padrão para LLMs).</summary>
    private const int CharsPerToken = 4;

    private AiContext() { }

    /// <summary>Identificador opcional da release associada ao contexto.</summary>
    public Guid? ReleaseId { get; private set; }

    /// <summary>Nome do serviço ao qual o contexto se refere.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Tipo de análise (ex: "change-analysis", "error-diagnosis", "test-generation").</summary>
    public string ContextType { get; private set; } = string.Empty;

    /// <summary>Payload JSON com os dados contextuais montados para a consulta.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Estimativa de tokens que o payload consumirá na consulta ao provedor.</summary>
    public int TokenEstimate { get; private set; }

    /// <summary>Data/hora UTC em que o contexto foi montado.</summary>
    public DateTimeOffset AssembledAt { get; private set; }

    /// <summary>
    /// Monta um novo contexto para consulta de IA com estimativa automática de tokens.
    /// O TokenEstimate é calculado com base no tamanho do payload (~4 chars/token).
    /// </summary>
    public static AiContext Assemble(
        string serviceName,
        string contextType,
        string payload,
        DateTimeOffset assembledAt,
        Guid? releaseId = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(contextType);
        Guard.Against.NullOrWhiteSpace(payload);

        var context = new AiContext
        {
            Id = AiContextId.New(),
            ServiceName = serviceName,
            ContextType = contextType,
            Payload = payload,
            AssembledAt = assembledAt,
            ReleaseId = releaseId
        };

        context.EstimateTokens();

        return context;
    }

    /// <summary>
    /// Calcula a estimativa de tokens com base no tamanho do payload.
    /// Utiliza heurística padrão de ~4 caracteres por token para LLMs.
    /// Encapsulado: chamado apenas pelo factory method para garantir invariante.
    /// </summary>
    private void EstimateTokens()
    {
        TokenEstimate = Payload.Length / CharsPerToken;
    }
}

/// <summary>Identificador fortemente tipado de AiContext.</summary>
public sealed record AiContextId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiContextId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiContextId From(Guid id) => new(id);
}
