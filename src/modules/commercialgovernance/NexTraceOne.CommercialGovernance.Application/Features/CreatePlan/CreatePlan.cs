using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.CommercialCatalog.Application.Abstractions;
using NexTraceOne.CommercialCatalog.Domain.Entities;
using NexTraceOne.CommercialCatalog.Domain.Errors;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.CommercialCatalog.Application.Features.CreatePlan;

/// <summary>
/// Feature: CreatePlan — operação de vendor ops para criar um novo plano comercial.
///
/// Usada pelo backoffice interno para definir ofertas comerciais com modelos
/// de licenciamento, limites de ativação e grace period configuráveis.
/// Valida unicidade do código do plano antes de persistir.
///
/// Permissão requerida: licensing:vendor:plan:create
/// </summary>
public static class CreatePlan
{
    /// <summary>Comando para criação de um novo plano comercial.</summary>
    public sealed record Command(
        string Code,
        string Name,
        CommercialModel CommercialModel,
        DeploymentModel DeploymentModel,
        int MaxActivations,
        int GracePeriodDays,
        string? Description = null,
        int? TrialDurationDays = null,
        string? PriceTag = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de plano.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxActivations).GreaterThan(0);
            RuleFor(x => x.GracePeriodDays).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TrialDurationDays)
                .GreaterThan(0)
                .When(x => x.TrialDurationDays.HasValue);
        }
    }

    /// <summary>
    /// Handler que cria um plano comercial após validar unicidade do código.
    /// </summary>
    public sealed class Handler(
        IPlanRepository planRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await planRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existing is not null)
            {
                return CommercialCatalogErrors.PlanCodeAlreadyExists(request.Code);
            }

            var plan = Plan.Create(
                request.Code,
                request.Name,
                request.CommercialModel,
                request.DeploymentModel,
                request.MaxActivations,
                request.GracePeriodDays,
                request.Description,
                request.TrialDurationDays,
                request.PriceTag);

            await planRepository.AddAsync(plan, cancellationToken);

            return new Response(plan.Id.Value, plan.Code, plan.Name);
        }
    }

    /// <summary>Resposta da criação de plano comercial.</summary>
    public sealed record Response(Guid PlanId, string Code, string Name);
}
