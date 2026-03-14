using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Errors;

namespace NexTraceOne.RuntimeIntelligence.Application.Features.GetObservabilityScore;

/// <summary>
/// Feature: GetObservabilityScore — obtém o perfil e score de maturidade de observabilidade de um serviço.
/// Retorna o score ponderado (0–1) e o detalhamento de cada capacidade (tracing, metrics, logging, alerting, dashboard).
/// Útil para identificar gaps de observabilidade e priorizar investimentos em monitoramento.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetObservabilityScore
{
    /// <summary>Query para obter o score de observabilidade de um serviço e ambiente.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de score de observabilidade.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que busca o perfil de observabilidade de um serviço.
    /// Retorna erro NotFound se o perfil ainda não foi avaliado para o serviço/ambiente.
    /// </summary>
    public sealed class Handler(
        IObservabilityProfileRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profile = await repository.GetByServiceAndEnvironmentAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (profile is null)
                return RuntimeIntelligenceErrors.ProfileNotFound($"{request.ServiceName}/{request.Environment}");

            return new Response(
                profile.Id.Value,
                profile.ServiceName,
                profile.Environment,
                profile.ObservabilityScore,
                profile.HasTracing,
                profile.HasMetrics,
                profile.HasLogging,
                profile.HasAlerting,
                profile.HasDashboard,
                profile.LastAssessedAt);
        }
    }

    /// <summary>Resposta com o score de observabilidade e detalhamento de capacidades do serviço.</summary>
    public sealed record Response(
        Guid ProfileId,
        string ServiceName,
        string Environment,
        decimal ObservabilityScore,
        bool HasTracing,
        bool HasMetrics,
        bool HasLogging,
        bool HasAlerting,
        bool HasDashboard,
        DateTimeOffset LastAssessedAt);
}
