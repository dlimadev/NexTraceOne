using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Define um campo personalizado para serviços no catálogo.
/// Permite que administradores de tenant adicionem campos extras ao registo de serviço.
///
/// Invariantes:
/// - FieldName não pode exceder 60 caracteres.
/// - FieldType deve ser um dos valores suportados.
/// - SortOrder deve ser não-negativo.
/// </summary>
public sealed class ServiceCustomField : AuditableEntity<ServiceCustomFieldId>
{
    private const int MaxNameLength = 60;
    private static readonly string[] ValidFieldTypes = ["Text", "Number", "Date", "Select", "MultiSelect", "Url", "Email"];

    private ServiceCustomField() { }

    public string TenantId { get; private set; } = string.Empty;
    public string FieldName { get; private set; } = string.Empty;
    public string FieldType { get; private set; } = "Text";
    public bool IsRequired { get; private set; }
    public string DefaultValue { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static ServiceCustomField Create(
        string tenantId,
        string fieldName,
        string fieldType,
        bool isRequired,
        string defaultValue,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(fieldName);
        Guard.Against.OutOfRange(fieldName.Length, nameof(fieldName), 1, MaxNameLength);
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var normalizedType = ValidFieldTypes.Contains(fieldType, StringComparer.OrdinalIgnoreCase)
            ? fieldType
            : "Text";

        var field = new ServiceCustomField
        {
            Id = new ServiceCustomFieldId(Guid.NewGuid()),
            TenantId = tenantId,
            FieldName = fieldName,
            FieldType = normalizedType,
            IsRequired = isRequired,
            DefaultValue = defaultValue ?? string.Empty,
            SortOrder = sortOrder,
        };

        field.SetCreated(createdAt, tenantId);
        return field;
    }

    public void UpdateDetails(string fieldName, bool isRequired, string defaultValue, int sortOrder, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(fieldName);
        Guard.Against.OutOfRange(fieldName.Length, nameof(fieldName), 1, MaxNameLength);
        Guard.Against.Negative(sortOrder, nameof(sortOrder));
        FieldName = fieldName;
        IsRequired = isRequired;
        DefaultValue = defaultValue ?? string.Empty;
        SortOrder = sortOrder;
        SetUpdated(updatedAt, CreatedBy);
    }
}
