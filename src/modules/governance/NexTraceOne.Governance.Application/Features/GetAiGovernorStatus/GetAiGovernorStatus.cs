using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetAiGovernorStatus;

/// <summary>
/// Feature: GetAiGovernorStatus — estado e configuração do AI Governor da plataforma.
/// Lê de IConfigurationResolutionService para chaves "ai.*".
/// Métricas de runtime são dados sintéticos (integração real pendente).
/// </summary>
public static class GetAiGovernorStatus
{
    /// <summary>Query sem parâmetros — retorna configuração e métricas do AI Governor.</summary>
    public sealed record Query() : IQuery<AiGovernorStatusResponse>;

    /// <summary>Comando para atualizar configuração do AI Governor.</summary>
    public sealed record UpdateAiGovernorConfig(
        int MaxConcurrency,
        int InferenceTimeoutSeconds,
        int QueueTimeoutSeconds,
        bool CircuitBreakerEnabled,
        int CircuitBreakerThreshold,
        int CircuitBreakerWindowSeconds,
        bool RateLimitEnabled,
        int RateLimitRequestsPerMinute) : ICommand<AiGovernorStatusResponse>;

    /// <summary>Handler de leitura do estado do AI Governor.</summary>
    public sealed class Handler(IConfigurationResolutionService configService) : IQueryHandler<Query, AiGovernorStatusResponse>
    {
        public async Task<Result<AiGovernorStatusResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxConcurrencyDto = await configService.ResolveEffectiveValueAsync(
                "ai.governor.max_concurrency", ConfigurationScope.System, null, cancellationToken);
            var timeoutDto = await configService.ResolveEffectiveValueAsync(
                "ai.governor.inference_timeout_seconds", ConfigurationScope.System, null, cancellationToken);
            var queueTimeoutDto = await configService.ResolveEffectiveValueAsync(
                "ai.governor.queue_timeout_seconds", ConfigurationScope.System, null, cancellationToken);
            var circuitBreakerDto = await configService.ResolveEffectiveValueAsync(
                "ai.governor.circuit_breaker.enabled", ConfigurationScope.System, null, cancellationToken);

            var maxConcurrency = int.TryParse(maxConcurrencyDto?.EffectiveValue, out var mc) ? mc : 5;
            var inferenceTimeout = int.TryParse(timeoutDto?.EffectiveValue, out var it) ? it : 30;
            var queueTimeout = int.TryParse(queueTimeoutDto?.EffectiveValue, out var qt) ? qt : 10;
            var circuitBreaker = !bool.TryParse(circuitBreakerDto?.EffectiveValue, out var cb) || cb;

            var config = new AiGovernorConfig(
                MaxConcurrency: maxConcurrency,
                InferenceTimeoutSeconds: inferenceTimeout,
                QueueTimeoutSeconds: queueTimeout,
                CircuitBreakerEnabled: circuitBreaker,
                CircuitBreakerThreshold: 5,
                CircuitBreakerWindowSeconds: 60,
                RateLimitEnabled: true,
                RateLimitRequestsPerMinute: 100);

            var metrics = new AiGovernorMetrics(
                ActiveInferences: 0,
                QueuedRequests: 0,
                CircuitBreakerOpen: false,
                TotalRequestsLastHour: 0,
                FailedRequestsLastHour: 0);

            var response = new AiGovernorStatusResponse(Config: config, Metrics: metrics);

            return Result<AiGovernorStatusResponse>.Success(response);
        }
    }

    /// <summary>Handler de atualização do AI Governor.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateAiGovernorConfig, AiGovernorStatusResponse>
    {
        public Task<Result<AiGovernorStatusResponse>> Handle(UpdateAiGovernorConfig request, CancellationToken cancellationToken)
        {
            var config = new AiGovernorConfig(
                MaxConcurrency: request.MaxConcurrency,
                InferenceTimeoutSeconds: request.InferenceTimeoutSeconds,
                QueueTimeoutSeconds: request.QueueTimeoutSeconds,
                CircuitBreakerEnabled: request.CircuitBreakerEnabled,
                CircuitBreakerThreshold: request.CircuitBreakerThreshold,
                CircuitBreakerWindowSeconds: request.CircuitBreakerWindowSeconds,
                RateLimitEnabled: request.RateLimitEnabled,
                RateLimitRequestsPerMinute: request.RateLimitRequestsPerMinute);

            var metrics = new AiGovernorMetrics(0, 0, false, 0, 0);

            return Task.FromResult(Result<AiGovernorStatusResponse>.Success(
                new AiGovernorStatusResponse(Config: config, Metrics: metrics)));
        }
    }

    /// <summary>Resposta de estado do AI Governor.</summary>
    public sealed record AiGovernorStatusResponse(AiGovernorConfig Config, AiGovernorMetrics Metrics);

    /// <summary>Configuração do AI Governor.</summary>
    public sealed record AiGovernorConfig(
        int MaxConcurrency,
        int InferenceTimeoutSeconds,
        int QueueTimeoutSeconds,
        bool CircuitBreakerEnabled,
        int CircuitBreakerThreshold,
        int CircuitBreakerWindowSeconds,
        bool RateLimitEnabled,
        int RateLimitRequestsPerMinute);

    /// <summary>Métricas de runtime do AI Governor.</summary>
    public sealed record AiGovernorMetrics(
        int ActiveInferences,
        int QueuedRequests,
        bool CircuitBreakerOpen,
        long TotalRequestsLastHour,
        long FailedRequestsLastHour);
}
