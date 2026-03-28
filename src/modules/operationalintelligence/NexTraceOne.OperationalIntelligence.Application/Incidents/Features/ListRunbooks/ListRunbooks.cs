using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;

/// <summary>
/// Feature: ListRunbooks — retorna a lista de runbooks operacionais disponíveis,
/// com suporte a filtragem por serviço, tipo de incidente e pesquisa textual.
/// </summary>
public static class ListRunbooks
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Query para listar runbooks com filtros opcionais.</summary>
    public sealed record Query(string? ServiceId, string? IncidentType, string? Search) : IQuery<Response>;

    /// <summary>Valida os parâmetros opcionais da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.IncidentType).MaximumLength(200).When(x => x.IncidentType is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>Handler que retorna a lista de runbooks via repositório.</summary>
    public sealed class Handler(IRunbookRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var runbooks = await repository.ListAsync(
                request.ServiceId,
                request.IncidentType,
                request.Search,
                cancellationToken);

            var dtos = runbooks
                .Select(r =>
                {
                    var stepCount = 0;
                    if (!string.IsNullOrWhiteSpace(r.StepsJson))
                    {
                        try
                        {
                            var steps = JsonSerializer.Deserialize<List<RunbookStepJson>>(r.StepsJson, JsonOptions);
                            stepCount = steps?.Count ?? 0;
                        }
                        catch (JsonException) { /* ignore malformed JSON */ }
                    }

                    return new RunbookSummaryDto(
                        r.Id.Value,
                        r.Title,
                        r.Description,
                        r.LinkedService,
                        r.LinkedIncidentType,
                        stepCount,
                        r.PublishedAt);
                })
                .ToList();

            return Result<Response>.Success(new Response(dtos.AsReadOnly()));
        }
    }

    /// <summary>Resposta com a lista de runbooks.</summary>
    public sealed record Response(IReadOnlyList<RunbookSummaryDto> Runbooks);

    /// <summary>Resumo de um runbook operacional.</summary>
    public sealed record RunbookSummaryDto(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        int StepCount,
        DateTimeOffset CreatedAt);

    private sealed class RunbookStepJson
    {
        [JsonPropertyName("stepOrder")] public int StepOrder { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    }
}
