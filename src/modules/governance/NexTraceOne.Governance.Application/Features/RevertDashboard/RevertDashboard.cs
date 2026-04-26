using FluentValidation;
using MediatR;
using System.Text.Json;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.RevertDashboard;

/// <summary>
/// Feature: RevertDashboard — reverte um dashboard para o estado de uma revisão anterior.
/// Cria automaticamente uma nova revisão do estado atual antes de aplicar o revert,
/// preservando a trilha de auditoria completa.
/// V3.1 — Dashboard Intelligence Foundation.
/// </summary>
public static class RevertDashboard
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Comando para reverter um dashboard a uma revisão anterior.</summary>
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId,
        int TargetRevisionNumber,
        string? RevertNote = null) : ICommand<Response>;

    /// <summary>Resposta indicando o sucesso e nova revisão criada após revert.</summary>
    public sealed record Response(
        Guid DashboardId,
        int RevertedFromRevision,
        int NewRevisionNumber,
        string Message);

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TargetRevisionNumber).GreaterThan(0)
                .WithMessage("Target revision number must be greater than 0.");
            RuleFor(x => x.RevertNote).MaximumLength(500).When(x => x.RevertNote is not null);
        }
    }

    /// <summary>Handler que reverte o dashboard e cria nova revisão.</summary>
    public sealed class Handler(
        ICustomDashboardRepository dashboardRepository,
        IDashboardRevisionRepository revisionRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await dashboardRepository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Custom dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden(
                    "CustomDashboard.Forbidden",
                    "Access to dashboard '{0}' is not allowed.",
                    request.DashboardId);

            if (dashboard.IsSystem)
                return Error.Business(
                    "CustomDashboard.SystemDashboardReadOnly",
                    "System dashboards cannot be modified.");

            var targetRevision = await revisionRepository.GetByRevisionNumberAsync(
                dashboard.Id, request.TargetRevisionNumber, cancellationToken);

            if (targetRevision is null)
                return Error.NotFound(
                    "DashboardRevision.NotFound",
                    "Revision '{0}' not found for dashboard '{1}'.",
                    request.TargetRevisionNumber, request.DashboardId);

            var now = clock.UtcNow;

            // Deserializar widgets da revisão alvo
            var widgets = JsonSerializer.Deserialize<List<DashboardWidget>>(
                targetRevision.WidgetsJson, _jsonOptions) ?? [];

            var variables = JsonSerializer.Deserialize<List<DashboardVariable>>(
                targetRevision.VariablesJson, _jsonOptions) ?? [];

            // Criar revisão do estado atual antes de reverter
            var preRevertWidgetsJson = JsonSerializer.Serialize(dashboard.Widgets, _jsonOptions);
            var preRevertVariablesJson = JsonSerializer.Serialize(dashboard.Variables, _jsonOptions);
            var preRevertSnapshot = dashboard.CreateRevisionSnapshot(
                preRevertWidgetsJson,
                preRevertVariablesJson,
                request.UserId,
                now,
                changeNote: $"[Auto] State before revert to revision {request.TargetRevisionNumber}");

            // Aplicar revert
            var revertNote = request.RevertNote ?? $"Reverted to revision {request.TargetRevisionNumber}";
            dashboard.Update(
                name: targetRevision.Name,
                description: targetRevision.Description,
                layout: targetRevision.Layout,
                widgets: widgets,
                teamId: dashboard.TeamId,
                now: now);
            dashboard.SetVariables(variables, now);

            // Criar revisão do estado pós-revert
            var postRevertWidgetsJson = JsonSerializer.Serialize(dashboard.Widgets, _jsonOptions);
            var postRevertVariablesJson = JsonSerializer.Serialize(dashboard.Variables, _jsonOptions);
            var postRevertSnapshot = dashboard.CreateRevisionSnapshot(
                postRevertWidgetsJson,
                postRevertVariablesJson,
                request.UserId,
                now,
                changeNote: revertNote);

            await revisionRepository.AddAsync(preRevertSnapshot, cancellationToken);
            await revisionRepository.AddAsync(postRevertSnapshot, cancellationToken);
            await dashboardRepository.UpdateAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                request.DashboardId,
                request.TargetRevisionNumber,
                dashboard.CurrentRevisionNumber,
                $"Dashboard reverted to revision {request.TargetRevisionNumber} successfully."));
        }
    }
}
