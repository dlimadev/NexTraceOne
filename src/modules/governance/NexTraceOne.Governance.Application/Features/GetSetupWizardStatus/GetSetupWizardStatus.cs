using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetSetupWizardStatus;

/// <summary>
/// Feature: GetSetupWizardStatus — retorna os passos já concluídos do SetupWizard.
/// Permite ao frontend restaurar o estado entre sessões.
/// F-04 — SetupWizard Persistence.
/// </summary>
public static class GetSetupWizardStatus
{
    public sealed record Query(string TenantId) : IQuery<Response>;

    public sealed record CompletedStepDto(
        string StepId,
        string DataJson,
        DateTimeOffset CompletedAt);

    public sealed record Response(
        IReadOnlyList<CompletedStepDto> CompletedSteps,
        bool IsFullyConfigured);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.TenantId).NotEmpty();
    }

    public sealed class Handler(ISetupWizardRepository repository) : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlySet<string> RequiredSteps =
            new HashSet<string> { "database", "security", "organization", "review" };

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var steps = await repository.ListByTenantAsync(request.TenantId, cancellationToken);

            var dtos = steps
                .Where(s => s.CompletedAt.HasValue)
                .Select(s => new CompletedStepDto(s.StepId, s.DataJson, s.CompletedAt!.Value))
                .ToList();

            var completedIds = dtos.Select(d => d.StepId).ToHashSet();
            var isFullyConfigured = RequiredSteps.All(r => completedIds.Contains(r));

            return Result<Response>.Success(new Response(dtos, isFullyConfigured));
        }
    }
}
