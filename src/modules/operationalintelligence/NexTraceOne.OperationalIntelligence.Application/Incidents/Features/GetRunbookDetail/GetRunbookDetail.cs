using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;

/// <summary>
/// Feature: GetRunbookDetail — retorna os detalhes completos de um runbook operacional,
/// incluindo passos, pré-condições, orientação pós-validação e metadados.
/// </summary>
public static class GetRunbookDetail
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Query para obter o detalhe de um runbook.</summary>
    public sealed record Query(string RunbookId) : IQuery<Response>;

    /// <summary>Valida o identificador do runbook.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RunbookId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o detalhe do runbook via repositório.</summary>
    public sealed class Handler(IRunbookRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Guid.TryParse(request.RunbookId, out var guid))
                return IncidentErrors.RunbookNotFound(request.RunbookId);

            var runbook = await repository.GetByIdAsync(guid, cancellationToken);
            if (runbook is null)
                return IncidentErrors.RunbookNotFound(request.RunbookId);

            var steps = DeserializeSteps(runbook.StepsJson);
            var prerequisites = DeserializePrerequisites(runbook.PrerequisitesJson);

            return Result<Response>.Success(new Response(
                runbook.Id.Value,
                runbook.Title,
                runbook.Description,
                runbook.LinkedService,
                runbook.LinkedIncidentType,
                steps,
                prerequisites,
                runbook.PostNotes,
                runbook.MaintainedBy,
                runbook.PublishedAt,
                runbook.LastReviewedAt));
        }

        private static IReadOnlyList<RunbookStepDto> DeserializeSteps(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                var steps = JsonSerializer.Deserialize<List<RunbookStepJson>>(json, JsonOptions);
                return steps?
                    .Select(s => new RunbookStepDto(s.StepOrder, s.Title, s.Description, s.IsOptional))
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static IReadOnlyList<string> DeserializePrerequisites(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    /// <summary>Resposta com os detalhes completos do runbook.</summary>
    public sealed record Response(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        IReadOnlyList<RunbookStepDto> Steps,
        IReadOnlyList<string> Preconditions,
        string? PostValidationGuidance,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    /// <summary>Passo individual do runbook.</summary>
    public sealed record RunbookStepDto(
        int StepOrder,
        string Title,
        string? Description,
        bool IsOptional);

    private sealed class RunbookStepJson
    {
        [JsonPropertyName("stepOrder")] public int StepOrder { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("isOptional")] public bool IsOptional { get; set; }
    }
}
