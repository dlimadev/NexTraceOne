using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.Promotion.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Promotion.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class PromotionDbContext(
    DbContextOptions<PromotionDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    // TODO: Adicionar DbSet<T> para cada entidade do módulo

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(PromotionDbContext).Assembly;
}
