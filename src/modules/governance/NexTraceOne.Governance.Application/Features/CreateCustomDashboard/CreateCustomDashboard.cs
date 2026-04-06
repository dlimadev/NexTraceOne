using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreateCustomDashboard;

/// <summary>
/// Feature: CreateCustomDashboard — cria um dashboard customizado para uma persona específica.
/// Permite que utilizadores configurem layouts e widgets de acordo com o seu papel funcional.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Builder visual para personas criarem dashboards customizados.
/// </summary>
public static class CreateCustomDashboard
{
    /// <summary>Comando para criar um novo dashboard customizado.</summary>
    public sealed record Command(
        string TenantId,
        string UserId,
        string Name,
        string? Description,
        string Layout,
        IReadOnlyList<string> WidgetIds,
        string Persona) : ICommand<Response>;

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
            RuleFor(x => x.WidgetIds).NotEmpty()
                .WithMessage("At least one widget must be selected.");
            RuleFor(x => x.WidgetIds.Count).LessThanOrEqualTo(20)
                .When(x => x.WidgetIds is not null)
                .WithMessage("A dashboard may contain at most 20 widgets.");
        }
    }

    /// <summary>Handler que gera a identidade do dashboard e retorna os metadados iniciais.</summary>
    public sealed class Handler(IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboardId = Guid.NewGuid();
            var now = clock.UtcNow;

            return Task.FromResult(Result<Response>.Success(new Response(
                DashboardId: dashboardId,
                Name: request.Name,
                Layout: request.Layout,
                WidgetCount: request.WidgetIds.Count,
                Persona: request.Persona,
                CreatedAt: now)));
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
