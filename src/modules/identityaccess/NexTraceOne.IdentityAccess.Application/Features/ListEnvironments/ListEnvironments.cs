using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.ListEnvironments;

/// <summary>
/// Feature: ListEnvironments — lista os ambientes disponíveis para o tenant atual.
///
/// Retorna os ambientes ativos do tenant (Development, Pre-Production, Production, etc.)
/// ordenados por SortOrder. Permite ao frontend exibir a lista de ambientes para
/// seleção e contexto operacional.
///
/// Regras:
/// - Apenas ambientes ativos são retornados.
/// - Ordenação por SortOrder crescente (menor = menos restrito).
/// - Requer autenticação e tenant context — enforcement feito pelo endpoint.
/// </summary>
public static class ListEnvironments
{
    /// <summary>Query para listar ambientes do tenant atual.</summary>
    public sealed record Query : IQuery<IReadOnlyList<EnvironmentResponse>>;

    /// <summary>Resumo de um ambiente para exibição no frontend.</summary>
    public sealed record EnvironmentResponse(
        Guid Id,
        string Name,
        string Slug,
        int SortOrder,
        bool IsActive);

    /// <summary>Handler que retorna os ambientes ativos do tenant atual.</summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IEnvironmentRepository environmentRepository) : IQueryHandler<Query, IReadOnlyList<EnvironmentResponse>>
    {
        public async Task<Result<IReadOnlyList<EnvironmentResponse>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);
            var environments = await environmentRepository.ListByTenantAsync(tenantId, cancellationToken);

            var result = environments
                .Select(e => new EnvironmentResponse(
                    e.Id.Value,
                    e.Name,
                    e.Slug,
                    e.SortOrder,
                    e.IsActive))
                .ToList();

            return Result<IReadOnlyList<EnvironmentResponse>>.Success(result);
        }
    }
}
