using FluentValidation;
using MediatR;
using System.Text.Json;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.UpdateCustomDashboard;

/// <summary>
/// Feature: UpdateCustomDashboard — atualiza nome, descrição, layout e widgets de um dashboard.
/// Apenas o criador ou um PlatformAdmin pode editar; dashboards de sistema não são editáveis
/// por utilizadores normais (validação de autorização é responsabilidade do endpoint).
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class UpdateCustomDashboard
{
    /// <summary>Input de widget para atualização do dashboard.</summary>
    public sealed record WidgetInput(
        string? ExistingWidgetId,
        string Type,
        int PosX,
        int PosY,
        int Width,
        int Height,
        string? ServiceId = null,
        string? TeamId = null,
        string? TimeRange = null,
        string? CustomTitle = null);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Comando para atualizar um dashboard customizado existente.</summary>
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId,
        string Name,
        string? Description,
        string Layout,
        IReadOnlyList<WidgetInput> Widgets,
        string? TeamId = null,
        string? ChangeNote = null) : ICommand;

    /// <summary>Validação do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidLayouts =
            ["single-column", "two-column", "three-column", "grid", "custom"];

        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Layout).NotEmpty()
                .Must(l => ValidLayouts.Contains(l))
                .WithMessage($"Layout must be one of: {string.Join(", ", ValidLayouts)}");
            RuleFor(x => x.Widgets).NotEmpty()
                .WithMessage("At least one widget is required.");
            RuleFor(x => x.Widgets.Count).LessThanOrEqualTo(20)
                .When(x => x.Widgets is not null)
                .WithMessage("A dashboard may contain at most 20 widgets.");
        }
    }

    /// <summary>Handler que atualiza, cria revisão e persiste o dashboard.</summary>
    public sealed class Handler(
        ICustomDashboardRepository repository,
        IDashboardRevisionRepository revisionRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
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

            var widgets = request.Widgets.Select(w => new DashboardWidget(
                WidgetId: string.IsNullOrWhiteSpace(w.ExistingWidgetId)
                    ? Guid.NewGuid().ToString()
                    : w.ExistingWidgetId,
                Type: w.Type,
                Position: new WidgetPosition(w.PosX, w.PosY, w.Width, w.Height),
                Config: new WidgetConfig(w.ServiceId, w.TeamId, w.TimeRange, w.CustomTitle)))
                .ToList();

            var now = clock.UtcNow;

            dashboard.Update(
                name: request.Name,
                description: request.Description,
                layout: request.Layout,
                widgets: widgets,
                teamId: request.TeamId,
                now: now);

            // Criar snapshot de revisão após cada Update (V3.1 — audit trail)
            var widgetsJson = JsonSerializer.Serialize(dashboard.Widgets, _jsonOptions);
            var variablesJson = JsonSerializer.Serialize(dashboard.Variables, _jsonOptions);
            var revision = dashboard.CreateRevisionSnapshot(
                widgetsJson,
                variablesJson,
                request.UserId,
                now,
                request.ChangeNote);

            await revisionRepository.AddAsync(revision, cancellationToken);
            await repository.UpdateAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
