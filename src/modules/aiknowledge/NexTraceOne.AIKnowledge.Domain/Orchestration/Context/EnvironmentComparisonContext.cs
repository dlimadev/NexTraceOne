using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que define o contexto para comparação de comportamento entre dois ambientes do mesmo tenant.
///
/// Permite à IA responder perguntas como:
/// - "O comportamento em QA está alinhado com o baseline de PROD?"
/// - "Existem regressões em STAGING comparando com PROD?"
/// - "O serviço X em UAT está pronto para ir a PROD?"
///
/// A comparação sempre ocorre dentro de um mesmo tenant para garantir isolamento.
/// Dados de tenants diferentes nunca são cruzados.
/// </summary>
public sealed class EnvironmentComparisonContext : ValueObject
{
    /// <summary>Contexto de execução da IA.</summary>
    public AiExecutionContext ExecutionContext { get; }

    /// <summary>Ambiente sendo avaliado (ex.: staging, UAT, QA).</summary>
    public EnvironmentId SubjectEnvironmentId { get; }

    /// <summary>Perfil do ambiente sendo avaliado.</summary>
    public EnvironmentProfile SubjectProfile { get; }

    /// <summary>Ambiente de referência para comparação (ex.: production, baseline).</summary>
    public EnvironmentId ReferenceEnvironmentId { get; }

    /// <summary>Perfil do ambiente de referência.</summary>
    public EnvironmentProfile ReferenceProfile { get; }

    /// <summary>
    /// Serviços específicos a comparar.
    /// Vazio indica que todos os serviços compartilhados entre os ambientes devem ser comparados.
    /// </summary>
    public IReadOnlyList<string> ServiceFilter { get; }

    /// <summary>Dimensões de comparação solicitadas.</summary>
    public IReadOnlyList<ComparisonDimension> Dimensions { get; }

    /// <summary>Janela de tempo para coleta de dados do ambiente de referência (baseline).</summary>
    public AiTimeWindow ReferenceWindow { get; }

    /// <summary>Janela de tempo para coleta de dados do ambiente avaliado (subject).</summary>
    public AiTimeWindow SubjectWindow { get; }

    private EnvironmentComparisonContext(
        AiExecutionContext executionContext,
        EnvironmentId subjectEnvironmentId,
        EnvironmentProfile subjectProfile,
        EnvironmentId referenceEnvironmentId,
        EnvironmentProfile referenceProfile,
        IReadOnlyList<string> serviceFilter,
        IReadOnlyList<ComparisonDimension> dimensions,
        AiTimeWindow referenceWindow,
        AiTimeWindow subjectWindow)
    {
        ExecutionContext = executionContext;
        SubjectEnvironmentId = subjectEnvironmentId;
        SubjectProfile = subjectProfile;
        ReferenceEnvironmentId = referenceEnvironmentId;
        ReferenceProfile = referenceProfile;
        ServiceFilter = serviceFilter;
        Dimensions = dimensions;
        ReferenceWindow = referenceWindow;
        SubjectWindow = subjectWindow;
    }

    /// <summary>
    /// Cria o contexto de comparação de ambientes.
    /// </summary>
    public static EnvironmentComparisonContext Create(
        AiExecutionContext executionContext,
        EnvironmentId subjectEnvironmentId,
        EnvironmentProfile subjectProfile,
        EnvironmentId referenceEnvironmentId,
        EnvironmentProfile referenceProfile,
        IEnumerable<string>? serviceFilter = null,
        IEnumerable<ComparisonDimension>? dimensions = null,
        AiTimeWindow? referenceWindow = null,
        AiTimeWindow? subjectWindow = null)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(subjectEnvironmentId);
        ArgumentNullException.ThrowIfNull(referenceEnvironmentId);

        if (subjectEnvironmentId.Value == referenceEnvironmentId.Value)
            throw new InvalidOperationException("Subject and reference environments must be different for comparison.");

        return new EnvironmentComparisonContext(
            executionContext,
            subjectEnvironmentId,
            subjectProfile,
            referenceEnvironmentId,
            referenceProfile,
            (serviceFilter ?? []).ToList().AsReadOnly(),
            (dimensions ?? ComparisonDimensionExtensions.AllDimensions).ToList().AsReadOnly(),
            referenceWindow ?? AiTimeWindow.LastDays(7),
            subjectWindow ?? AiTimeWindow.LastDays(2));
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExecutionContext.TenantId;
        yield return SubjectEnvironmentId;
        yield return ReferenceEnvironmentId;
    }
}

/// <summary>
/// Dimensões de comparação disponíveis para análise cross-environment.
/// </summary>
public enum ComparisonDimension
{
    /// <summary>Métricas de performance e latência.</summary>
    Performance = 1,

    /// <summary>Taxa de erros e exceções.</summary>
    ErrorRate = 2,

    /// <summary>Disponibilidade e uptime.</summary>
    Availability = 3,

    /// <summary>Compatibilidade de contratos de API e eventos.</summary>
    ContractCompatibility = 4,

    /// <summary>Topologia e dependências de serviços.</summary>
    Topology = 5,

    /// <summary>Padrões de incidentes recentes.</summary>
    IncidentPatterns = 6,

    /// <summary>Cobertura e resultado de testes automatizados.</summary>
    TestCoverage = 7
}

public static class ComparisonDimensionExtensions
{
    /// <summary>Todas as dimensões disponíveis para análise completa.</summary>
    public static readonly IReadOnlyList<ComparisonDimension> AllDimensions =
        Enum.GetValues<ComparisonDimension>().ToList().AsReadOnly();
}
