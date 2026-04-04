using System.Text.Json;
using System.Text.Json.Serialization;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateRunbook;

/// <summary>
/// Feature: UpdateRunbook — atualiza um runbook operacional existente.
/// Mantém a identidade e data de publicação original, atualizando conteúdo e passos.
/// </summary>
public static class UpdateRunbook
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Comando para atualizar um runbook existente.</summary>
    public sealed record Command(
        Guid RunbookId,
        string Title,
        string Description,
        string? LinkedService,
        string? LinkedIncidentType,
        IReadOnlyList<UpdateRunbookStepDto>? Steps,
        IReadOnlyList<string>? Prerequisites,
        string? PostNotes,
        string MaintainedBy) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios e limites do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RunbookId).NotEmpty();
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

    /// <summary>Handler que atualiza o runbook via repositório.</summary>
    public sealed class Handler(
        IRunbookRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var runbook = await repository.GetByIdForUpdateAsync(request.RunbookId, cancellationToken);

            if (runbook is null)
                return Error.NotFound("runbook.not_found", "Runbook {0} not found.", request.RunbookId);

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

            runbook.Update(
                request.Title,
                request.Description,
                request.LinkedService,
                request.LinkedIncidentType,
                stepsJson,
                prerequisitesJson,
                request.PostNotes,
                request.MaintainedBy,
                clock.UtcNow);

            await repository.UpdateAsync(runbook, cancellationToken);

            return Result<Response>.Success(new Response(runbook.Id.Value, clock.UtcNow));
        }
    }

    /// <summary>Resposta com o identificador e data de atualização do runbook.</summary>
    public sealed record Response(Guid RunbookId, DateTimeOffset UpdatedAt);

    /// <summary>Passo estruturado de entrada para atualização do runbook.</summary>
    public sealed record UpdateRunbookStepDto(
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
