using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.CreateCustomDashboard;

/// <summary>
/// Feature: CreateCustomDashboard — cria um dashboard customizado persistido.
/// Cada dashboard é associado a um tenant, utilizador e persona.
/// Os widgets incluem posição no grid e configuração contextual (serviço, equipa, período).
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class CreateCustomDashboard
{
    /// <summary>Input para um widget individual com posição e configuração contextual.</summary>
    public sealed record WidgetInput(
        string Type,
        int PosX,
        int PosY,
        int Width,
        int Height,
        string? ServiceId = null,
        string? TeamId = null,
        string? TimeRange = null,
        string? CustomTitle = null);

    /// <summary>Comando para criar um novo dashboard customizado.</summary>
    public sealed record Command(
        string TenantId,
        string UserId,
        string Name,
        string? Description,
        string Layout,
        IReadOnlyList<WidgetInput> Widgets,
        string Persona,
        string? TeamId = null,
        bool IsSystem = false) : ICommand<Response>;

    /// <summary>Validação do comando de criação de dashboard customizado.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidLayouts =
            ["single-column", "two-column", "three-column", "grid", "custom"];

        private static readonly string[] ValidPersonas =
            ["Engineer", "TechLead", "Architect", "Product", "Executive", "PlatformAdmin", "Auditor"];

        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Layout).NotEmpty()
                .Must(l => ValidLayouts.Contains(l))
                .WithMessage($"Layout must be one of: {string.Join(", ", ValidLayouts)}");
            RuleFor(x => x.Persona).NotEmpty().MaximumLength(50)
                .Must(p => ValidPersonas.Contains(p))
                .WithMessage($"Persona must be one of: {string.Join(", ", ValidPersonas)}");
            RuleFor(x => x.Widgets).NotEmpty()
                .WithMessage("At least one widget must be selected.");
            RuleFor(x => x.Widgets.Count).LessThanOrEqualTo(20)
                .When(x => x.Widgets is not null)
                .WithMessage("A dashboard may contain at most 20 widgets.");
        }
    }

    /// <summary>Handler que cria e persiste um novo dashboard customizado.</summary>
    public sealed class Handler(
        ICustomDashboardRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var widgets = request.Widgets.Select(w => new DashboardWidget(
                WidgetId: Guid.NewGuid().ToString(),
                Type: w.Type,
                Position: new WidgetPosition(w.PosX, w.PosY, w.Width, w.Height),
                Config: new WidgetConfig(w.ServiceId, w.TeamId, w.TimeRange, w.CustomTitle)))
                .ToList();

            var dashboard = CustomDashboard.Create(
                name: request.Name,
                description: request.Description,
                layout: request.Layout,
                persona: request.Persona,
                widgets: widgets,
                tenantId: request.TenantId,
                userId: request.UserId,
                now: now,
                teamId: request.TeamId,
                isSystem: request.IsSystem);

            await repository.AddAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                DashboardId: dashboard.Id.Value,
                Name: dashboard.Name,
                Layout: dashboard.Layout,
                WidgetCount: dashboard.WidgetCount,
                Persona: dashboard.Persona,
                CreatedAt: dashboard.CreatedAt));
        }
    }

    /// <summary>Resposta com os metadados do dashboard criado.</summary>
    public sealed record Response(
        Guid DashboardId,
        string Name,
        string Layout,
        int WidgetCount,
        string Persona,
        DateTimeOffset CreatedAt);
}
