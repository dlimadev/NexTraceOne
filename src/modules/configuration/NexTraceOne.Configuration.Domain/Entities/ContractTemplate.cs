using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Template de contrato personalizado por tenant.
/// Admins definem templates pré-preenchidos para REST, SOAP e Event contracts.
/// Utilizado no wizard de criação de contratos como "Choose template" step.
/// </summary>
public sealed class ContractTemplate : AuditableEntity<ContractTemplateId>
{
    private const int MaxNameLength = 100;
    private static readonly string[] ValidContractTypes = ["REST", "SOAP", "Event", "AsyncAPI", "Background"];

    private ContractTemplate() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ContractType { get; private set; } = string.Empty;
    public string TemplateJson { get; private set; } = "{}";
    public string Description { get; private set; } = string.Empty;
    public string TemplateCreatedBy { get; private set; } = string.Empty;
    public bool IsBuiltIn { get; private set; }

    public static ContractTemplate Create(
        string tenantId,
        string name,
        string contractType,
        string templateJson,
        string description,
        string createdBy,
        bool isBuiltIn,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(contractType);
        Guard.Against.NullOrWhiteSpace(createdBy);

        var normalizedType = ValidContractTypes.Contains(contractType, StringComparer.OrdinalIgnoreCase)
            ? contractType
            : "REST";

        var template = new ContractTemplate
        {
            Id = new ContractTemplateId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            ContractType = normalizedType,
            TemplateJson = string.IsNullOrWhiteSpace(templateJson) ? "{}" : templateJson,
            Description = description ?? string.Empty,
            TemplateCreatedBy = createdBy,
            IsBuiltIn = isBuiltIn,
        };
        template.SetCreated(createdAt, string.Empty);
        template.SetUpdated(createdAt, string.Empty);
        return template;
    }
}
