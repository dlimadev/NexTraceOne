using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;

namespace NexTraceOne.Identity.Application.Features.ListPermissions;

/// <summary>
/// Feature: ListPermissions — lista todas as permissões registradas no sistema.
/// </summary>
public static class ListPermissions
{
    /// <summary>Query para listar permissões do sistema.</summary>
    public sealed record Query : IQuery<IReadOnlyList<PermissionResponse>>;

    /// <summary>Handler que retorna todas as permissões registradas.</summary>
    public sealed class Handler(IPermissionRepository permissionRepository) : IQueryHandler<Query, IReadOnlyList<PermissionResponse>>
    {
        public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var permissions = await permissionRepository.GetAllAsync(cancellationToken);

            var result = permissions.Select(p => new PermissionResponse(
                p.Id.Value,
                p.Code,
                p.Name,
                p.Module)).ToList();

            return result;
        }
    }

    /// <summary>Resumo de uma permissão registrada.</summary>
    public sealed record PermissionResponse(Guid Id, string Code, string Name, string Module);
}
