using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.DiffCanonicalEntityVersions;

/// <summary>
/// Feature: DiffCanonicalEntityVersions — calcula o diff entre duas versões de uma entidade canónica.
/// Compara os conteúdos JSON dos schemas e identifica campos adicionados, removidos e modificados.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DiffCanonicalEntityVersions
{
    /// <summary>Query de diff entre versões de entidade canónica.</summary>
    public sealed record Query(
        Guid CanonicalEntityId,
        string FromVersion,
        string ToVersion) : IQuery<Response>;

    /// <summary>Valida os parâmetros de diff.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CanonicalEntityId).NotEmpty();
            RuleFor(x => x.FromVersion).NotEmpty();
            RuleFor(x => x.ToVersion).NotEmpty();
        }
    }

    /// <summary>Handler que computa o diff entre duas versões de schema canónico.</summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository,
        ICanonicalEntityVersionRepository versionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entityId = CanonicalEntityId.From(request.CanonicalEntityId);

            var entity = await entityRepository.GetByIdAsync(entityId, cancellationToken);
            if (entity is null)
                return ContractsErrors.CanonicalEntityNotFound(request.CanonicalEntityId.ToString());

            var fromVersion = await versionRepository.GetByVersionAsync(entityId, request.FromVersion, cancellationToken);
            if (fromVersion is null)
                return ContractsErrors.CanonicalEntityVersionNotFound(request.FromVersion);

            var toVersion = await versionRepository.GetByVersionAsync(entityId, request.ToVersion, cancellationToken);
            if (toVersion is null)
                return ContractsErrors.CanonicalEntityVersionNotFound(request.ToVersion);

            var (added, removed, modified) = ComputeJsonDiff(fromVersion.SchemaContent, toVersion.SchemaContent);

            return new Response(
                request.CanonicalEntityId,
                entity.Name,
                request.FromVersion,
                request.ToVersion,
                added,
                removed,
                modified);
        }

        private static (List<string> Added, List<string> Removed, List<string> Modified) ComputeJsonDiff(
            string fromJson, string toJson)
        {
            var added = new List<string>();
            var removed = new List<string>();
            var modified = new List<string>();

            var fromFields = ExtractFields(fromJson);
            var toFields = ExtractFields(toJson);

            foreach (var field in toFields)
            {
                if (!fromFields.ContainsKey(field.Key))
                    added.Add(field.Key);
                else if (fromFields[field.Key] != field.Value)
                    modified.Add(field.Key);
            }

            foreach (var field in fromFields)
            {
                if (!toFields.ContainsKey(field.Key))
                    removed.Add(field.Key);
            }

            return (added, removed, modified);
        }

        private static Dictionary<string, string> ExtractFields(string json)
        {
            var fields = new Dictionary<string, string>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                FlattenElement(doc.RootElement, string.Empty, fields);
            }
            catch (JsonException)
            {
                // Invalid JSON returns empty field set
            }

            return fields;
        }

        private static void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> fields)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenElement(property.Value, key, fields);
                    }
                    break;

                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        FlattenElement(item, $"{prefix}[{index}]", fields);
                        index++;
                    }
                    break;

                default:
                    fields[prefix] = element.ToString() ?? string.Empty;
                    break;
            }
        }
    }

    /// <summary>Descrição de um campo alterado no diff.</summary>
    public sealed record DiffField(string Path);

    /// <summary>Resposta do diff entre versões de entidade canónica.</summary>
    public sealed record Response(
        Guid CanonicalEntityId,
        string EntityName,
        string FromVersion,
        string ToVersion,
        IReadOnlyList<string> AddedFields,
        IReadOnlyList<string> RemovedFields,
        IReadOnlyList<string> ModifiedFields);
}
