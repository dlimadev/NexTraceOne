using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ComputeObservabilityDebt;

/// <summary>
/// Feature: ComputeObservabilityDebt — avalia e cria/atualiza o perfil de observabilidade de um serviço.
/// Recebe as capacidades de observabilidade presentes (tracing, metrics, logging, alerting, dashboard)
/// e cria um novo ObservabilityProfile com o score recalculado automaticamente pelo domínio.
/// Se já existir um perfil para o serviço/ambiente, cria nova avaliação (histórico de evolução).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeObservabilityDebt
{
    /// <summary>Comando para avaliar a maturidade de observabilidade de um serviço.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        bool HasTracing,
        bool HasMetrics,
        bool HasLogging,
        bool HasAlerting,
        bool HasDashboard) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de avaliação de observabilidade.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que cria um perfil de observabilidade via factory method do domínio.
    /// O score é calculado automaticamente como soma ponderada das capacidades presentes.
    /// Pesos: tracing=0.25, metrics=0.25, logging=0.20, alerting=0.15, dashboard=0.15.
    /// </summary>
    public sealed class Handler(
        IObservabilityProfileRepository repository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var profile = ObservabilityProfile.Assess(
                request.ServiceName,
                request.Environment,
                request.HasTracing,
                request.HasMetrics,
                request.HasLogging,
                request.HasAlerting,
                request.HasDashboard,
                now);

            repository.Add(profile);
            await unitOfWork.CommitAsync(cancellationToken);

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

    /// <summary>Resposta da avaliação de observabilidade com score calculado e capacidades.</summary>
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
