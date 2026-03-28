using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy;

/// <summary>
/// Feature: ConfigureExternalAIPolicy — validação de configuração de provider.
/// </summary>
public static class ConfigureExternalAIPolicy
{
    public sealed record Command(
        string ProviderId,
        string ProviderType,
        string EndpointUrl,
        string? ApiKey,
        string ModelName,
        bool TestConnectivity = false) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProviderId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ProviderType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EndpointUrl).NotEmpty().MaximumLength(1_000);
            RuleFor(x => x.ModelName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ApiKey).MaximumLength(1_000).When(x => x.ApiKey is not null);
        }
    }

    public sealed class Handler(
        IAiProviderFactory providerFactory) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var errors = new List<FieldValidationError>();

            ValidateEndpoint(request.EndpointUrl, errors);
            ValidateApiKey(request.ProviderType, request.ApiKey, errors);
            ValidateModelName(request.ModelName, errors);

            var connectivity = new ConnectivityValidationResult(false, false, null, null);
            if (request.TestConnectivity && errors.Count == 0)
            {
                var provider = providerFactory.GetProvider(request.ProviderId);
                if (provider is null)
                {
                    connectivity = connectivity with
                    {
                        Checked = true,
                        IsHealthy = false,
                        ErrorMessage = $"Provider '{request.ProviderId}' is not registered."
                    };
                    errors.Add(new FieldValidationError("providerId", "Provider is not registered in runtime factory."));
                }
                else
                {
                    var health = await provider.CheckHealthAsync(cancellationToken);
                    connectivity = new ConnectivityValidationResult(
                        true,
                        health.IsHealthy,
                        health.ResponseTime.HasValue
                            ? Math.Round(health.ResponseTime.Value.TotalMilliseconds, 2)
                            : null,
                        health.IsHealthy ? null : health.Message);

                    if (!health.IsHealthy)
                    {
                        errors.Add(new FieldValidationError(
                            "connectivity",
                            health.Message ?? "Provider connectivity check failed."));
                    }
                }
            }

            return new Response(
                errors.Count == 0,
                errors,
                connectivity);
        }

        private static void ValidateEndpoint(string endpointUrl, ICollection<FieldValidationError> errors)
        {
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add(new FieldValidationError("endpointUrl", "Endpoint URL must be a valid absolute HTTP/HTTPS URL."));
            }
        }

        private static void ValidateApiKey(string providerType, string? apiKey, ICollection<FieldValidationError> errors)
        {
            var requiresApiKey = string.Equals(providerType, "openai", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(providerType, "azureopenai", StringComparison.OrdinalIgnoreCase);

            if (!requiresApiKey)
                return;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                errors.Add(new FieldValidationError("apiKey", "API key is required for this provider type."));
                return;
            }

            if (apiKey.Length < 16)
            {
                errors.Add(new FieldValidationError("apiKey", "API key format appears invalid."));
            }
        }

        private static void ValidateModelName(string modelName, ICollection<FieldValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                errors.Add(new FieldValidationError("modelName", "Model name is required."));
                return;
            }

            if (modelName.Any(char.IsWhiteSpace))
            {
                errors.Add(new FieldValidationError("modelName", "Model name must not contain whitespace."));
            }
        }
    }

    public sealed record Response(
        bool IsValid,
        IReadOnlyList<FieldValidationError> Errors,
        ConnectivityValidationResult Connectivity);

    public sealed record FieldValidationError(
        string Field,
        string Message);

    public sealed record ConnectivityValidationResult(
        bool Checked,
        bool IsHealthy,
        double? LatencyMs,
        string? ErrorMessage);
}
