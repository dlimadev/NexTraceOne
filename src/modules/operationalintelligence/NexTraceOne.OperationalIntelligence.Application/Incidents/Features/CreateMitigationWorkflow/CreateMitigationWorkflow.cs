using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;

/// <summary>
/// Feature: CreateMitigationWorkflow — cria e persiste um novo workflow de mitigação para um incidente,
/// definindo tipo de ação, nível de risco, passos e associação a runbooks.
/// </summary>
public static class CreateMitigationWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Comando para criar um workflow de mitigação.</summary>
    public sealed record Command(
        string IncidentId,
        string Title,
        MitigationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        Guid? LinkedRunbookId,
        IReadOnlyList<CreateStepDto>? Steps) : ICommand<Response>;

    /// <summary>Passo a incluir na criação do workflow.</summary>
    public sealed record CreateStepDto(int StepOrder, string Title, string? Description);

    /// <summary>Valida os campos obrigatórios do comando de criação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ActionType).IsInEnum();
            RuleFor(x => x.RiskLevel).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que persiste o workflow de mitigação via repositório dedicado.
    /// Usa IIncidentStore apenas para verificar a existência do incidente.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IMitigationWorkflowRepository workflowRepository,
        ICurrentUser currentUser,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!store.IncidentExists(request.IncidentId))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            var id = MitigationWorkflowRecordId.New();

            var stepsJson = request.Steps is { Count: > 0 }
                ? JsonSerializer.Serialize(
                    request.Steps.Select(s => new WorkflowStepJson
                    {
                        StepOrder = s.StepOrder,
                        Title = s.Title,
                        Description = s.Description,
                    }).ToList(),
                    JsonOptions)
                : null;

            var workflow = MitigationWorkflowRecord.Create(
                id,
                request.IncidentId,
                request.Title,
                MitigationWorkflowStatus.Draft,
                request.ActionType,
                request.RiskLevel,
                request.RequiresApproval,
                currentUser.Id,
                request.LinkedRunbookId,
                stepsJson);

            await workflowRepository.AddAsync(workflow, cancellationToken);

            return Result<Response>.Success(new Response(id.Value, MitigationWorkflowStatus.Draft, workflow.CreatedAt));
        }
    }

    /// <summary>Resposta da criação do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        MitigationWorkflowStatus Status,
        DateTimeOffset CreatedAt);

    private sealed class WorkflowStepJson
    {
        [JsonPropertyName("stepOrder")] public int StepOrder { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("isCompleted")] public bool IsCompleted { get; set; }
    }
}
