using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;

/// <summary>
/// Feature: QueryExternalAISimple — health check essencial dos providers externos configurados.
/// </summary>
public static class QueryExternalAISimple
{
    public sealed record Command(
        string? ProviderId = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProviderId)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.ProviderId));
        }
    }

    public sealed class Handler(
        IAiProviderHealthService healthService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var checkedAt = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.ProviderId))
            {
                var providerResult = await healthService.CheckProviderAsync(request.ProviderId, cancellationToken);
                return new Response(
                    [MapProviderStatus(providerResult, checkedAt)],
                    1);
            }

            var results = await healthService.CheckAllProvidersAsync(cancellationToken);
            var providers = results
                .Select(result => MapProviderStatus(result, checkedAt))
                .ToList();

            return new Response(providers, providers.Count);
        }

        private static ProviderHealthStatusItem MapProviderStatus(
            AiProviderHealthResult result,
            DateTimeOffset checkedAt)
        {
            var latencyMs = result.ResponseTime.HasValue
                ? Math.Round(result.ResponseTime.Value.TotalMilliseconds, 2)
                : (double?)null;

            var status = result.IsHealthy
                ? ProviderHealthStatus.Healthy.ToString()
                : ProviderHealthStatus.Unhealthy.ToString();

            return new ProviderHealthStatusItem(
                result.ProviderId,
                status,
                latencyMs,
                checkedAt,
                result.IsHealthy ? null : result.Message);
        }
    }

    public sealed record Response(
        IReadOnlyList<ProviderHealthStatusItem> Providers,
        int TotalProviders);

    public sealed record ProviderHealthStatusItem(
        string ProviderId,
        string Status,
        double? LatencyMs,
        DateTimeOffset LastCheckedAt,
        string? ErrorMessage);
}
