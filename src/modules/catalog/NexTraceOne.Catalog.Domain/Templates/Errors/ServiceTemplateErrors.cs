using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Domain.Templates.Errors;

/// <summary>
/// Erros de domínio do subsistema de ServiceTemplate.
/// </summary>
public static class ServiceTemplateErrors
{
    /// <summary>Template não encontrado.</summary>
    public static Error NotFound(Guid templateId) =>
        Error.NotFound("ServiceTemplate.NotFound", $"Service template '{templateId}' was not found.");

    /// <summary>Template não encontrado por slug.</summary>
    public static Error NotFoundBySlug(string slug) =>
        Error.NotFound("ServiceTemplate.NotFoundBySlug", $"Service template with slug '{slug}' was not found.");

    /// <summary>Slug duplicado — já existe um template com este identificador.</summary>
    public static Error DuplicateSlug(string slug) =>
        Error.Conflict("ServiceTemplate.DuplicateSlug", $"A service template with slug '{slug}' already exists.");

    /// <summary>Template está desativado e não pode ser usado para scaffolding.</summary>
    public static Error TemplateDisabled(Guid templateId) =>
        Error.Validation("ServiceTemplate.Disabled", $"Service template '{templateId}' is disabled and cannot be used for scaffolding.");

    /// <summary>Nome de serviço scaffolded inválido.</summary>
    public static Error InvalidServiceName(string name) =>
        Error.Validation("ServiceTemplate.InvalidServiceName", $"Service name '{name}' is invalid. Use lowercase alphanumeric with hyphens only.");
}
