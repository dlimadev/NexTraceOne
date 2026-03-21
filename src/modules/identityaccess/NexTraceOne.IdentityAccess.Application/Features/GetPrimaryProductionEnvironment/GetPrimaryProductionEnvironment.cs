using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.GetPrimaryProductionEnvironment;

/// <summary>
/// Feature: GetPrimaryProductionEnvironment — obtém o ambiente designado como produção principal do tenant.
///
/// Este query é usado pela IA, pelo motor de risco de release e pelos relatórios de readiness
/// para identificar o ambiente de referência ao comparar com ambientes não produtivos.
///
/// Retorna null se nenhum ambiente produtivo principal estiver designado (estado válido durante onboarding).
/// </summary>
public static class GetPrimaryProductionEnvironment
{
    /// <summary>Query para obter o ambiente produtivo principal do tenant.</summary>
    public sealed record Query : IQuery<Response?>;

    /// <summary>Dados do ambiente produtivo principal.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        string Slug,
        string Profile,
        string Criticality,
        bool IsProductionLike,
        string? Code,
        string? Region);

    /// <summary>Handler que retorna o ambiente produtivo principal ativo do tenant.</summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IEnvironmentRepository environmentRepository) : IQueryHandler<Query, Response?>
    {
        private static readonly Dictionary<EnvironmentProfile, string> ProfileNames =
            System.Enum.GetValues<EnvironmentProfile>()
                .ToDictionary(p => p, p => p.ToString().ToLowerInvariant());

        private static readonly Dictionary<EnvironmentCriticality, string> CriticalityNames =
            System.Enum.GetValues<EnvironmentCriticality>()
                .ToDictionary(c => c, c => c.ToString().ToLowerInvariant());

        public async Task<Result<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);
            var environment = await environmentRepository.GetPrimaryProductionAsync(tenantId, cancellationToken);

            if (environment is null)
                return Result<Response?>.Success(null);

            return Result<Response?>.Success(new Response(
                environment.Id.Value,
                environment.Name,
                environment.Slug,
                ProfileNames.GetValueOrDefault(environment.Profile, "unknown"),
                CriticalityNames.GetValueOrDefault(environment.Criticality, "low"),
                environment.IsProductionLike,
                environment.Code,
                environment.Region));
        }
    }
}
