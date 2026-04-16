using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetEnvironmentPromotionPath;

/// <summary>
/// Feature: GetEnvironmentPromotionPath — retorna o caminho de promoção de uma release entre ambientes.
///
/// Responde à pergunta: "por quais ambientes esta release já passou, e em que estado está em cada um?"
/// O resultado permite visualizar a "pipeline" de promoção: Dev → Staging → Production,
/// mostrando aprovações pendentes, bloqueios e promoções concluídas.
///
/// Algoritmo:
///   1. Lista todos os PromotionRequests para o releaseId fornecido.
///   2. Agrupa por targetEnvironment (cada ambiente recebe o seu step).
///   3. Infere a ordem dos ambientes pelo momento de criação do request.
///   4. Indica o status de cada step (Pending / Approved / Blocked / InProgress).
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetEnvironmentPromotionPath
{
    /// <summary>Query para obter o caminho de promoção de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que compõe o caminho de promoção a partir das PromotionRequests existentes.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IDeploymentEnvironmentRepository environmentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotions = await requestRepository.ListByReleaseIdAsync(request.ReleaseId, cancellationToken);

            // Resolve environment names in bulk
            var envIds = promotions
                .SelectMany(p => new[] { p.SourceEnvironmentId, p.TargetEnvironmentId })
                .Distinct()
                .ToList();

            var envNames = new Dictionary<Guid, string>();
            foreach (var envId in envIds)
            {
                var env = await environmentRepository.GetByIdAsync(envId, cancellationToken);
                if (env is not null)
                    envNames[envId.Value] = env.Name;
            }

            var steps = promotions
                .OrderBy(p => p.RequestedAt)
                .Select(p => new PromotionPathStep(
                    PromotionRequestId: p.Id.Value,
                    SourceEnvironment: envNames.GetValueOrDefault(p.SourceEnvironmentId.Value, p.SourceEnvironmentId.Value.ToString()),
                    TargetEnvironment: envNames.GetValueOrDefault(p.TargetEnvironmentId.Value, p.TargetEnvironmentId.Value.ToString()),
                    Status: p.Status.ToString(),
                    RequestedBy: p.RequestedBy,
                    RequestedAt: p.RequestedAt,
                    CompletedAt: p.CompletedAt,
                    Justification: p.Justification))
                .ToList();

            var currentEnvironment = DeriveCurrentEnvironment(steps);
            var isFullyPromoted = steps.Count > 0
                && steps.All(s => s.Status == nameof(PromotionStatus.Approved));

            return new Response(
                ReleaseId: request.ReleaseId,
                Steps: steps,
                CurrentEnvironment: currentEnvironment,
                IsFullyPromoted: isFullyPromoted,
                HasBlockers: steps.Any(s => s.Status == nameof(PromotionStatus.Blocked)),
                TotalSteps: steps.Count,
                CompletedSteps: steps.Count(s => s.Status == nameof(PromotionStatus.Approved)));
        }

        private static string? DeriveCurrentEnvironment(IReadOnlyList<PromotionPathStep> steps)
        {
            // O ambiente "atual" é o target do último step aprovado, ou o source do primeiro step pendente.
            var lastApproved = steps.LastOrDefault(s => s.Status == "Approved");
            if (lastApproved is not null)
                return lastApproved.TargetEnvironment;

            var firstPending = steps.FirstOrDefault();
            return firstPending?.SourceEnvironment;
        }
    }

    /// <summary>Step de promoção no caminho de environments.</summary>
    public sealed record PromotionPathStep(
        Guid PromotionRequestId,
        string SourceEnvironment,
        string TargetEnvironment,
        string Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        DateTimeOffset? CompletedAt,
        string? Justification);

    /// <summary>Resposta com o caminho completo de promoção de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        IReadOnlyList<PromotionPathStep> Steps,
        string? CurrentEnvironment,
        bool IsFullyPromoted,
        bool HasBlockers,
        int TotalSteps,
        int CompletedSteps);
}
