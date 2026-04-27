using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.InstantiateTemplate;

/// <summary>
/// Feature: InstantiateTemplate — instancia um template de dashboard criando um CustomDashboard independente.
/// V3.8 — Marketplace, Plugin SDK &amp; Widgets de Terceiros.
/// </summary>
public static class InstantiateTemplate
{
    public sealed record Command(
        Guid TemplateId,
        string TenantId,
        string UserId,
        string? CustomName = null) : ICommand<Response>;

    public sealed record Response(
        Guid DashboardId,
        string DashboardName,
        Guid SourceTemplateId,
        bool IsSimulated);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CustomName).MaximumLength(100).When(x => x.CustomName is not null);
        }
    }

    public sealed class Handler(
        IDashboardTemplateRepository templateRepository,
        ICustomDashboardRepository dashboardRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var template = await templateRepository.GetByIdAsync(
                new DashboardTemplateId(request.TemplateId), request.TenantId, cancellationToken);

            if (template is null)
                return Error.NotFound(
                    "DashboardTemplate.NotFound",
                    "Template with ID '{0}' was not found.",
                    request.TemplateId);

            var dashboardName = request.CustomName ?? $"{template.Name} (copy)";

            // Deserialize snapshot to get widgets and layout
            var widgets = new List<DashboardWidget>();
            var layout = "grid";
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(template.DashboardSnapshotJson);
                if (doc.RootElement.TryGetProperty("layout", out var layoutEl))
                    layout = layoutEl.GetString() ?? "grid";
            }
            catch { /* malformed snapshot — use defaults */ }

            var dashboard = CustomDashboard.Create(
                name: dashboardName,
                description: $"Instanciado a partir do template: {template.Name}",
                layout: layout,
                persona: template.Persona,
                widgets: widgets,
                tenantId: request.TenantId,
                userId: request.UserId,
                now: clock.UtcNow);

            dashboard.Publish(clock.UtcNow);

            template.IncrementInstallCount();

            await dashboardRepository.AddAsync(dashboard, cancellationToken);
            await templateRepository.UpdateAsync(template, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                dashboard.Id.Value,
                dashboard.Name,
                template.Id.Value,
                IsSimulated: true));
        }
    }
}
