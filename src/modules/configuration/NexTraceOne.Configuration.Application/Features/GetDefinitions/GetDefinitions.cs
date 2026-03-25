using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;

namespace NexTraceOne.Configuration.Application.Features.GetDefinitions;

/// <summary>
/// Feature: GetDefinitions — returns all registered configuration definitions.
/// Used by management UI to display the full configuration catalog.
/// </summary>
public static class GetDefinitions
{
    /// <summary>Query to retrieve all configuration definitions.</summary>
    public sealed record Query : IQuery<List<ConfigurationDefinitionDto>>;

    /// <summary>Handler that fetches definitions from the repository and maps to DTOs.</summary>
    public sealed class Handler(
        IConfigurationDefinitionRepository definitionRepository)
        : IQueryHandler<Query, List<ConfigurationDefinitionDto>>
    {
        public async Task<Result<List<ConfigurationDefinitionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var definitions = await definitionRepository.GetAllAsync(cancellationToken);

            var dtos = definitions.Select(d => new ConfigurationDefinitionDto(
                Key: d.Key,
                DisplayName: d.DisplayName,
                Description: d.Description,
                Category: d.Category.ToString(),
                AllowedScopes: d.AllowedScopes.Select(s => s.ToString()).ToArray(),
                DefaultValue: d.DefaultValue,
                ValueType: d.ValueType.ToString(),
                IsSensitive: d.IsSensitive,
                IsEditable: d.IsEditable,
                IsInheritable: d.IsInheritable,
                ValidationRules: d.ValidationRules,
                UiEditorType: d.UiEditorType,
                SortOrder: d.SortOrder,
                IsDeprecated: d.IsDeprecated,
                DeprecatedMessage: d.DeprecatedMessage)).ToList();

            return dtos;
        }
    }
}
