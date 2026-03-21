using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o contexto operacional resolvido de uma requisição ou operação.
/// Combina TenantId, EnvironmentId e EnvironmentProfile como contexto mínimo obrigatório.
///
/// Este é o contexto base que toda operação com escopo operacional (observabilidade, incidentes,
/// topologia, IA) deve carregar para garantir isolamento correto por tenant e ambiente.
///
/// Princípio: TenantId + EnvironmentId = contexto mínimo.
/// EnvironmentProfile = comportamento esperado da operação dentro desse contexto.
/// </summary>
public sealed class TenantEnvironmentContext : ValueObject
{
    /// <summary>Identificador do tenant proprietário do ambiente.</summary>
    public TenantId TenantId { get; }

    /// <summary>Identificador do ambiente resolvido.</summary>
    public EnvironmentId EnvironmentId { get; }

    /// <summary>Perfil operacional do ambiente resolvido.</summary>
    public EnvironmentProfile Profile { get; }

    /// <summary>Criticidade do ambiente.</summary>
    public EnvironmentCriticality Criticality { get; }

    /// <summary>Indica se o ambiente tem comportamento similar à produção.</summary>
    public bool IsProductionLike { get; }

    /// <summary>Indica se o ambiente está ativo no momento da resolução.</summary>
    public bool IsActive { get; }

    private TenantEnvironmentContext(
        TenantId tenantId,
        EnvironmentId environmentId,
        EnvironmentProfile profile,
        EnvironmentCriticality criticality,
        bool isProductionLike,
        bool isActive)
    {
        TenantId = tenantId;
        EnvironmentId = environmentId;
        Profile = profile;
        Criticality = criticality;
        IsProductionLike = isProductionLike;
        IsActive = isActive;
    }

    /// <summary>
    /// Cria um TenantEnvironmentContext a partir de uma entidade Environment resolvida.
    /// Este é o ponto de entrada principal — o contexto deve sempre ser resolvido
    /// a partir de uma entidade persistida, nunca inferido de parâmetros isolados.
    /// </summary>
    public static TenantEnvironmentContext From(DomainEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return new TenantEnvironmentContext(
            environment.TenantId,
            environment.Id,
            environment.Profile,
            environment.Criticality,
            environment.IsProductionLike,
            environment.IsActive);
    }

    /// <summary>
    /// Cria um TenantEnvironmentContext a partir de valores explícitos.
    /// Usado em cenários de reconstrução a partir de dados serializados ou de cache.
    /// </summary>
    public static TenantEnvironmentContext Create(
        TenantId tenantId,
        EnvironmentId environmentId,
        EnvironmentProfile profile,
        EnvironmentCriticality criticality,
        bool isProductionLike,
        bool isActive)
        => new(tenantId, environmentId, profile, criticality, isProductionLike, isActive);

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TenantId;
        yield return EnvironmentId;
    }

    /// <summary>
    /// Verifica se este contexto é compatível com uma operação que requer ambiente de produção.
    /// </summary>
    public bool RequiresProductionSafeguards()
        => IsProductionLike || Criticality >= EnvironmentCriticality.High;

    /// <summary>
    /// Verifica se a IA pode realizar análise profunda neste ambiente.
    /// Ambientes não-produtivos permitem análise mais profunda para detecção de regressões.
    /// </summary>
    public bool AllowsDeepAiAnalysis()
        => IsActive && !IsProductionLike;

    /// <summary>
    /// Verifica se este contexto representa um ambiente candidato a ser comparado com produção.
    /// Verdadeiro para perfis Staging, UserAcceptanceTesting e ambientes com alta criticidade.
    /// </summary>
    public bool IsPreProductionCandidate()
        => Profile is EnvironmentProfile.Staging or EnvironmentProfile.UserAcceptanceTesting
           || (Criticality >= EnvironmentCriticality.High && !IsProductionLike);

    public override string ToString()
        => $"[Tenant={TenantId.Value:N}, Env={EnvironmentId.Value:N}, Profile={Profile}, Criticality={Criticality}]";
}
