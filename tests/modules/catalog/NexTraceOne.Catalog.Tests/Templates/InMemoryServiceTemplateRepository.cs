using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Tests.Templates;

/// <summary>
/// Implementação in-memory do IServiceTemplateRepository para testes unitários.
/// Carrega um conjunto de templates de seed para simular o comportamento do EF Core.
/// </summary>
internal sealed class InMemoryServiceTemplateRepository : IServiceTemplateRepository
{
    private readonly List<ServiceTemplate> _templates;

    public InMemoryServiceTemplateRepository()
    {
        _templates = new List<ServiceTemplate>(BuildSeedTemplates());
    }

    public Task<ServiceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.FirstOrDefault(t => t.Id.Value == id));

    public Task<ServiceTemplate?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<ServiceTemplate>> ListAsync(
        bool? isActive,
        TemplateServiceType? serviceType,
        TemplateLanguage? language,
        string? search,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = _templates.AsEnumerable();

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        if (serviceType.HasValue)
            query = query.Where(t => t.ServiceType == serviceType.Value);

        if (language.HasValue)
            query = query.Where(t => t.Language == language.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value || t.TenantId is null);

        IReadOnlyList<ServiceTemplate> result = query.ToList().AsReadOnly();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.Any(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(ServiceTemplate template, CancellationToken cancellationToken = default)
    {
        _templates.Add(template);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceTemplate template, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Devolve o primeiro template da seed (para testes).</summary>
    public ServiceTemplate GetFirstTemplate() => _templates[0];

    private static IEnumerable<ServiceTemplate> BuildSeedTemplates()
    {
        yield return ServiceTemplate.Create(
            slug: "dotnet-rest-api",
            displayName: ".NET REST API Template",
            description: "Standard .NET REST API with OpenAPI contract, governance and ownership pre-configured.",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.DotNet,
            defaultDomain: "platform",
            defaultTeam: "platform-team",
            tags: new[] { "dotnet", "rest", "openapi" }.ToList().AsReadOnly(),
            scaffoldingManifestJson: """[{"Path":"README.md","Content":"# {{ServiceName}}"},{"Path":".nextraceone.json","Content":"{\"service\":\"{{ServiceName}}\"}"}]""");

        yield return ServiceTemplate.Create(
            slug: "kafka-event-consumer",
            displayName: "Kafka Event Consumer Template",
            description: "Event-driven consumer service with AsyncAPI contract template.",
            version: "1.0.0",
            serviceType: TemplateServiceType.EventDriven,
            language: TemplateLanguage.DotNet,
            defaultDomain: "events",
            defaultTeam: "integration-team",
            tags: new[] { "kafka", "events", "asyncapi" }.ToList().AsReadOnly());

        yield return ServiceTemplate.Create(
            slug: "nodejs-rest-api",
            displayName: "Node.js REST API Template",
            description: "Node.js REST API with Express and OpenAPI contract.",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.NodeJs,
            defaultDomain: "platform",
            defaultTeam: "frontend-team",
            tags: new[] { "nodejs", "express", "rest" }.ToList().AsReadOnly());
    }
}
