using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateChaosExperiment;

/// <summary>
/// Feature: CreateChaosExperiment — gera um plano de experimento de chaos engineering sem persistência.
/// Produz um conjunto de passos sequenciais, avalia o nível de risco e lista verificações de segurança
/// que devem ser confirmadas antes da execução real do experimento.
///
/// Tipos suportados: latency-injection, error-injection, cpu-stress, memory-stress, pod-kill, network-partition.
/// Classificação de risco: pod-kill e network-partition → High; cpu-stress e memory-stress → Medium;
/// latency-injection e error-injection → Low.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateChaosExperiment
{
    /// <summary>Comando para criar um plano de experimento de chaos engineering.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        string ExperimentType,
        string? Description,
        int DurationSeconds = 60,
        decimal TargetPercentage = 10m) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de criação de experimento de chaos.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExperimentType).NotEmpty();
            RuleFor(x => x.DurationSeconds)
                .InclusiveBetween(10, 3600)
                .WithMessage("Duration must be between 10 and 3600 seconds.");
            RuleFor(x => x.TargetPercentage)
                .InclusiveBetween(1m, 100m)
                .WithMessage("Target percentage must be between 1 and 100.");
        }
    }

    /// <summary>
    /// Handler que computa o plano do experimento de chaos de forma pura, sem persistência.
    /// Mapeia o tipo de experimento para passos e avalia o nível de risco.
    /// </summary>
    public sealed class Handler(IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        private static readonly IReadOnlyList<string> SafetyChecks = new[]
        {
            "Ensure rollback plan exists",
            "Confirm monitoring alerts active",
            "Verify backup/recovery procedures",
            "Notify on-call team before experiment",
        };

        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var experimentId = Guid.NewGuid();
            var steps = MapSteps(request.ExperimentType);
            var riskLevel = AssessRisk(request.ExperimentType);

            var response = new Response(
                experimentId,
                request.ServiceName,
                request.Environment,
                request.ExperimentType,
                steps,
                riskLevel,
                request.DurationSeconds,
                request.TargetPercentage,
                SafetyChecks,
                dateTimeProvider.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        /// <summary>Mapeia o tipo de experimento para a sequência de passos de execução.</summary>
        private static IReadOnlyList<string> MapSteps(string experimentType)
            => experimentType switch
            {
                "latency-injection" => new[]
                {
                    "Configure latency injection parameters",
                    "Apply latency to target service endpoints",
                    "Monitor error rate increase",
                    "Verify timeout handling",
                    "Restore normal latency",
                },
                "error-injection" => new[]
                {
                    "Configure error injection rate",
                    "Apply HTTP error responses to endpoints",
                    "Observe client retry behavior",
                    "Monitor downstream impact",
                    "Remove error injection",
                },
                "cpu-stress" => new[]
                {
                    "Verify monitoring alerts are active",
                    "Apply CPU load to target percentage",
                    "Monitor service response times",
                    "Check autoscaling behavior",
                    "Restore normal CPU usage",
                },
                "memory-stress" => new[]
                {
                    "Verify memory baseline",
                    "Apply memory pressure to target",
                    "Monitor garbage collection metrics",
                    "Check OOM recovery behavior",
                    "Restore normal memory usage",
                },
                "pod-kill" => new[]
                {
                    "Identify target pod(s)",
                    "Verify replica count",
                    "Terminate target pod",
                    "Verify pod restart",
                    "Confirm service recovery",
                },
                "network-partition" => new[]
                {
                    "Identify network segments",
                    "Apply network partition rules",
                    "Monitor connectivity errors",
                    "Verify circuit breaker activation",
                    "Restore network connectivity",
                },
                _ => new[]
                {
                    "Prepare experiment environment",
                    "Execute experiment",
                    "Monitor impact",
                    "Restore normal state",
                },
            };

        /// <summary>Avalia o nível de risco com base no tipo de experimento.</summary>
        private static string AssessRisk(string experimentType)
            => experimentType switch
            {
                "pod-kill" or "network-partition" => "High",
                "cpu-stress" or "memory-stress" => "Medium",
                _ => "Low",
            };
    }

    /// <summary>Resposta com o plano completo do experimento de chaos.</summary>
    public sealed record Response(
        Guid ExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        IReadOnlyList<string> Steps,
        string RiskLevel,
        int EstimatedDurationSeconds,
        decimal TargetPercentage,
        IReadOnlyList<string> SafetyChecks,
        DateTimeOffset CreatedAt);
}
