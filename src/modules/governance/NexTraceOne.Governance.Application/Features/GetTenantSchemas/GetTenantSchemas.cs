using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Infrastructure.MultiTenancy;

namespace NexTraceOne.Governance.Application.Features.GetTenantSchemas;

/// <summary>
/// Feature: GetTenantSchemas — lista schemas PostgreSQL por tenant e permite provisionamento.
/// Fornece ao PlatformAdmin visibilidade completa sobre o estado de isolamento schema-per-tenant.
/// Suporta listagem, verificação individual e provisionamento de novos schemas.
/// </summary>
public static class GetTenantSchemas
{
    /// <summary>Query para listar todos os schemas de tenant na base de dados.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que consulta schemas existentes via ITenantSchemaManager.</summary>
    public sealed class Handler(ITenantSchemaManager schemaManager) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var schemas = await schemaManager.ListTenantSchemasAsync(cancellationToken);

            var items = schemas.Select(slug => new TenantSchemaInfo(
                TenantSlug: slug,
                SchemaName: $"tenant_{slug}",
                SearchPath: schemaManager.GetSearchPath(slug)
            )).ToList();

            return Result<Response>.Success(new Response(
                TotalSchemas: items.Count,
                Schemas: items,
                CheckedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Response com lista de schemas de tenants.</summary>
    public sealed record Response(
        int TotalSchemas,
        IReadOnlyList<TenantSchemaInfo> Schemas,
        DateTimeOffset CheckedAt);

    /// <summary>Informação de um schema de tenant.</summary>
    public sealed record TenantSchemaInfo(
        string TenantSlug,
        string SchemaName,
        string SearchPath);
}

/// <summary>
/// Command: ProvisionTenantSchema — cria ou garante a existência do schema de um tenant.
/// Idempotente: se o schema já existir, retorna sem erro.
/// </summary>
public static class ProvisionTenantSchema
{
    /// <summary>Command para provisionar o schema de um tenant.</summary>
    public sealed record Command(string TenantSlug) : ICommand<Response>;

    /// <summary>Handler que cria o schema via ITenantSchemaManager.</summary>
    public sealed class Handler(ITenantSchemaManager schemaManager) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenantSlug))
                return Error.Validation("TENANT_SLUG_REQUIRED", "Tenant slug is required.");

            var created = await schemaManager.EnsureSchemaCreatedAsync(request.TenantSlug, cancellationToken);

            return Result<Response>.Success(new Response(
                TenantSlug: request.TenantSlug,
                SchemaName: $"tenant_{request.TenantSlug}",
                WasCreated: created,
                ProvisionedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Response do provisionamento de schema.</summary>
    public sealed record Response(
        string TenantSlug,
        string SchemaName,
        bool WasCreated,
        DateTimeOffset ProvisionedAt);
}
