using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.SimulatePolicyApplication;

/// <summary>
/// Feature: SimulatePolicyApplication — simula a aplicação de uma política como código,
/// calculando quantos serviços seriam afectados e quantos ficariam não-conformes.
/// Persiste o resultado de simulação na definição para consulta posterior.
/// "Se aplicar esta política, X serviços ficam non-compliant."
/// </summary>
public static class SimulatePolicyApplication
{
    public sealed record Command(
        string PolicyName,
        /// <summary>Lista de IDs de serviços que seriam alvos da política.</summary>
        IReadOnlyList<string> ServiceIds,
        /// <summary>Lista de IDs de serviços que NÃO cumprem a política.</summary>
        IReadOnlyList<string> NonCompliantServiceIds) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceIds).NotNull();
            RuleFor(x => x.NonCompliantServiceIds).NotNull();
        }
    }

    public sealed class Handler(
        IPolicyAsCodeRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var definition = await repository.GetByNameAsync(request.PolicyName, cancellationToken);
            if (definition is null)
                return Error.NotFound("POLICY_AS_CODE_NOT_FOUND", "Policy definition '{0}' not found.", request.PolicyName);

            var affectedCount = request.ServiceIds.Count;
            var nonCompliantCount = request.NonCompliantServiceIds.Count;

            if (nonCompliantCount > affectedCount)
                return Error.Validation("SIMULATE_INVALID_COUNTS",
                    "NonCompliantServiceIds count ({0}) cannot exceed ServiceIds count ({1}).",
                    nonCompliantCount, affectedCount);

            definition.RecordSimulationResult(affectedCount, nonCompliantCount, clock.UtcNow);
            await repository.UpdateAsync(definition, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var compliancePercent = affectedCount == 0
                ? 100m
                : Math.Round((affectedCount - nonCompliantCount) / (decimal)affectedCount * 100, 1);

            return Result<Response>.Success(new Response(
                definition.Id.Value,
                definition.Name,
                definition.EnforcementMode,
                affectedCount,
                nonCompliantCount,
                compliancePercent,
                request.NonCompliantServiceIds,
                clock.UtcNow));
        }
    }

    public sealed record Response(
        Guid PolicyId,
        string PolicyName,
        PolicyEnforcementMode EnforcementMode,
        int AffectedServices,
        int NonCompliantServices,
        decimal CompliancePercent,
        IReadOnlyList<string> NonCompliantServiceIds,
        DateTimeOffset SimulatedAt);
}
