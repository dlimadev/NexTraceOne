using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Repositories;

/// <summary>
/// Repositório EF Core de ServiceTemplate.
/// Implementa IServiceTemplateRepository com consultas específicas de negócio.
/// Cada método de escrita chama SaveChangesAsync — padrão de repositório auto-commit
/// utilizado quando o handler não injeta IUnitOfWork explicitamente.
/// </summary>
internal sealed class EfServiceTemplateRepository(TemplatesDbContext context) : IServiceTemplateRepository
{
    /// <inheritdoc />
    public async Task<ServiceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.ServiceTemplates
            .FirstOrDefaultAsync(t => t.Id == new ServiceTemplateId(id), cancellationToken);

    /// <inheritdoc />
    public async Task<ServiceTemplate?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.ServiceTemplates
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceTemplate>> ListAsync(
        bool? isActive,
        TemplateServiceType? serviceType,
        TemplateLanguage? language,
        string? search,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = context.ServiceTemplates.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        if (serviceType.HasValue)
            query = query.Where(t => t.ServiceType == serviceType.Value);

        if (language.HasValue)
            query = query.Where(t => t.Language == language.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.DisplayName, pattern) ||
                EF.Functions.ILike(t.Description, pattern) ||
                EF.Functions.ILike(t.Slug, pattern) ||
                EF.Functions.ILike(t.DefaultDomain, pattern) ||
                EF.Functions.ILike(t.DefaultTeam, pattern));
        }

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value || t.TenantId == null);

        return await query
            .OrderBy(t => t.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.ServiceTemplates.AnyAsync(t => t.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ServiceTemplate template, CancellationToken cancellationToken = default)
    {
        await context.ServiceTemplates.AddAsync(template, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ServiceTemplate template, CancellationToken cancellationToken = default)
    {
        context.ServiceTemplates.Update(template);
        await context.SaveChangesAsync(cancellationToken);
    }
}
