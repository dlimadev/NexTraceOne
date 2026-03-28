using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateRunbook;

/// <summary>
/// Feature: CreateRunbook — persiste um novo runbook operacional no repositório.
/// Aceita título, descrição, serviço associado, tipo de incidente, passos estruturados,
/// pré-condições, orientação pós-validação e responsável pela manutenção.
/// </summary>
public static class CreateRunbook
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Comando para criar um novo runbook.</summary>
    public sealed record Command(
        string Title,
        string Description,
        string? LinkedService,
        string? LinkedIncidentType,
        IReadOnlyList<CreateRunbookStepDto>? Steps,
        IReadOnlyList<string>? Prerequisites,
        string? PostNotes,
        string MaintainedBy) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios e limites do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.MaintainedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.LinkedService).MaximumLength(200).When(x => x.LinkedService is not null);
            RuleFor(x => x.LinkedIncidentType).MaximumLength(200).When(x => x.LinkedIncidentType is not null);
            RuleFor(x => x.PostNotes).MaximumLength(4000).When(x => x.PostNotes is not null);
            RuleForEach(x => x.Steps).ChildRules(step =>
            {
                step.RuleFor(s => s.Title).NotEmpty().MaximumLength(500);
                step.RuleFor(s => s.Description).MaximumLength(4000).When(s => s.Description is not null);
            }).When(x => x.Steps is not null);
        }
    }

    /// <summary>Handler que persiste o runbook via repositório.</summary>
    public sealed class Handler(
        IRunbookRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var id = RunbookRecordId.New();

            var stepsJson = request.Steps is { Count: > 0 }
                ? JsonSerializer.Serialize(
                    request.Steps.Select((s, i) => new RunbookStepJson
                    {
                        StepOrder = s.StepOrder > 0 ? s.StepOrder : i + 1,
                        Title = s.Title,
                        Description = s.Description,
                        IsOptional = s.IsOptional,
                    }).ToList(),
                    JsonOptions)
                : null;

            var prerequisitesJson = request.Prerequisites is { Count: > 0 }
                ? JsonSerializer.Serialize(request.Prerequisites, JsonOptions)
                : null;

            var runbook = RunbookRecord.Create(
                id,
                request.Title,
                request.Description,
                request.LinkedService,
                request.LinkedIncidentType,
                stepsJson,
                prerequisitesJson,
                request.PostNotes,
                request.MaintainedBy,
                now);

            await repository.AddAsync(runbook, cancellationToken);

            return Result<Response>.Success(new Response(id.Value, now));
        }
    }

    /// <summary>Resposta com o identificador e data de criação do runbook.</summary>
    public sealed record Response(Guid RunbookId, DateTimeOffset CreatedAt);

    /// <summary>Passo estruturado de entrada para criação do runbook.</summary>
    public sealed record CreateRunbookStepDto(
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
