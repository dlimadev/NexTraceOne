using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.CreateServiceCostProfile;

/// <summary>
/// Feature: CreateServiceCostProfile — cria um perfil de custo para um serviço e ambiente.
/// O perfil é necessário para que a verificação de anomalias de custo (AlertCostAnomaly) funcione.
/// Inclui suporte para orçamento mensal e limiar percentual de alerta.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateServiceCostProfile
{
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal AlertThresholdPercent = 80m,
        decimal? MonthlyBudget = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AlertThresholdPercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.MonthlyBudget).GreaterThanOrEqualTo(0m).When(x => x.MonthlyBudget.HasValue);
        }
    }

    public sealed class Handler(
        IServiceCostProfileRepository repository,
        ICostIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Idempotent: if profile already exists, return it without error
            var existing = await repository.GetByServiceAndEnvironmentAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (existing is not null)
            {
                return Result<Response>.Success(new Response(
                    existing.Id.Value,
                    existing.ServiceName,
                    existing.Environment,
                    existing.MonthlyBudget,
                    existing.AlertThresholdPercent,
                    existing.CurrentMonthCost,
                    existing.BudgetUsagePercent,
                    existing.IsOverBudget));
            }

            var profile = ServiceCostProfile.Create(
                request.ServiceName,
                request.Environment,
                request.AlertThresholdPercent,
                clock.UtcNow,
                request.MonthlyBudget);

            repository.Add(profile);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                profile.Id.Value,
                profile.ServiceName,
                profile.Environment,
                profile.MonthlyBudget,
                profile.AlertThresholdPercent,
                profile.CurrentMonthCost,
                profile.BudgetUsagePercent,
                profile.IsOverBudget));
        }
    }

    public sealed record Response(
        Guid ProfileId,
        string ServiceName,
        string Environment,
        decimal? MonthlyBudget,
        decimal AlertThresholdPercent,
        decimal CurrentMonthCost,
        decimal? BudgetUsagePercent,
        bool IsOverBudget);
}
