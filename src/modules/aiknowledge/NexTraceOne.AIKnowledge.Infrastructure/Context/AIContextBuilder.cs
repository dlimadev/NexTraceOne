using NexTraceOne.AIKnowledge.Application.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Context;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação de IAIContextBuilder.
/// Constrói o AiExecutionContext a partir do contexto de execução operacional atual.
///
/// Garante que toda operação de IA receba:
/// - TenantId e EnvironmentId validados do contexto ativo
/// - Escopos de dados controlados pelo backend
/// - Contexto do usuário e persona
///
/// Segurança: o frontend não pode expandir escopos — eles são determinados
/// com base no perfil do ambiente e nas permissões do usuário.
/// </summary>
internal sealed class AIContextBuilder(
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IEnvironmentContextAccessor environmentContextAccessor,
    ITenantEnvironmentContextResolver contextResolver) : IAIContextBuilder
{
    /// <inheritdoc />
    public Task<AiExecutionContext> BuildAsync(
        string moduleContext,
        CancellationToken cancellationToken = default)
    {
        var tenantId = new TenantId(currentTenant.Id);
        var environmentId = environmentContextAccessor.IsResolved
            ? environmentContextAccessor.EnvironmentId
            : new EnvironmentId(Guid.Empty);

        var profile = environmentContextAccessor.IsResolved
            ? environmentContextAccessor.Profile
            : EnvironmentProfile.Development;

        var isProductionLike = environmentContextAccessor.IsResolved
            && environmentContextAccessor.IsProductionLike;

        var userContext = BuildUserContext();
        var scopes = DetermineAllowedScopes(profile, isProductionLike, currentUser);

        var executionContext = AiExecutionContext.Create(
            tenantId,
            environmentId,
            profile,
            isProductionLike,
            userContext,
            moduleContext,
            scopes);

        return Task.FromResult(executionContext);
    }

    /// <inheritdoc />
    public async Task<AiExecutionContext> BuildForAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        string moduleContext,
        CancellationToken cancellationToken = default)
    {
        var tenantEnvContext = await contextResolver.ResolveAsync(
            tenantId, environmentId, cancellationToken);

        var profile = tenantEnvContext?.Profile ?? EnvironmentProfile.Development;
        var isProductionLike = tenantEnvContext?.IsProductionLike ?? false;

        var userContext = BuildUserContext();
        var scopes = DetermineAllowedScopes(profile, isProductionLike, currentUser);

        return AiExecutionContext.Create(
            tenantId,
            environmentId,
            profile,
            isProductionLike,
            userContext,
            moduleContext,
            scopes);
    }

    private AiUserContext BuildUserContext()
        => new(
            currentUser.Id,
            currentUser.Name,
            DeterminePersona(currentUser),
            DetermineRoles(currentUser));

    private static string DeterminePersona(ICurrentUser user)
    {
        // Inferência de persona baseada em permissões existentes usando prioridade decrescente.
        // Um usuário com múltiplas permissões recebe a persona mais elevada (most-privileged wins).
        // Em fases futuras, a persona virá explicitamente do token JWT como claim dedicado.
        if (user.HasPermission("platform:admin"))
            return "PlatformAdmin";
        if (user.HasPermission("governance:manage"))
            return "Architect";
        if (user.HasPermission("services:manage"))
            return "TechLead";
        if (user.HasPermission("aiknowledge:write"))
            return "Engineer";

        return "Engineer";
    }

    private static IReadOnlyList<string> DetermineRoles(ICurrentUser user)
    {
        var roles = new List<string>();
        if (user.HasPermission("platform:admin")) roles.Add("PlatformAdmin");
        if (user.HasPermission("governance:manage")) roles.Add("Architect");
        if (user.HasPermission("services:manage")) roles.Add("TechLead");
        return roles.Count > 0 ? roles : ["Engineer"];
    }

    /// <summary>
    /// Determina os escopos de dados permitidos para a IA com base no perfil do ambiente
    /// e nas permissões do usuário.
    ///
    /// Regras:
    /// - Ambientes não produtivos com permissão de comparação: escopos completos
    /// - Ambientes de produção: escopos básicos (sem cross-environment comparison)
    /// - Usuários sem permissão especial: escopos padrão
    /// </summary>
    private static IEnumerable<string> DetermineAllowedScopes(
        EnvironmentProfile profile,
        bool isProductionLike,
        ICurrentUser user)
    {
        var isNonProduction = !isProductionLike;
        var canCompareEnvironments = user.HasPermission("aiknowledge:cross-environment")
                                     || user.HasPermission("aiknowledge:admin");

        if (isNonProduction && canCompareEnvironments)
            return AiDataScope.FullAnalysisScopes;

        if (isNonProduction)
            return [.. AiDataScope.DefaultScopes, AiDataScope.PromotionAnalysis];

        return AiDataScope.DefaultScopes;
    }
}
