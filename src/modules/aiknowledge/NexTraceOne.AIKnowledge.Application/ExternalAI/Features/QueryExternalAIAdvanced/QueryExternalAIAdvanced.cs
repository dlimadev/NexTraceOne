using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced;

/// <summary>
/// Feature: QueryExternalAIAdvanced — seleção de modelos reais a partir do Model Registry.
/// </summary>
public static class QueryExternalAIAdvanced
{
    public sealed record Command(
        string? Provider = null,
        string? Capability = null,
        bool ActiveOnly = true) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Provider)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Provider));

            RuleFor(x => x.Capability)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Capability));
        }
    }

    public sealed class Handler(
        IAiModelRepository modelRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            ModelStatus? statusFilter = request.ActiveOnly ? ModelStatus.Active : null;
            var models = await modelRepository.ListAsync(
                request.Provider,
                modelType: null,
                statusFilter,
                isInternal: null,
                cancellationToken);

            var filtered = models
                .Select(model => new
                {
                    Model = model,
                    ParsedCapabilities = ParseCapabilities(model.Capabilities)
                })
                .Where(x => HasCapability(x.ParsedCapabilities, request.Capability))
                .Select(x => new ModelItem(
                    x.Model.Id.Value,
                    x.Model.Name,
                    x.Model.Provider,
                    x.ParsedCapabilities,
                    x.Model.Status == ModelStatus.Active ? "active" : "inactive",
                    x.Model.ContextWindow))
                .ToList();

            return new Response(filtered, filtered.Count);
        }

        private static bool HasCapability(IReadOnlyList<string> parsedCapabilities, string? requestedCapability)
        {
            if (string.IsNullOrWhiteSpace(requestedCapability))
                return true;

            return parsedCapabilities.Any(capability =>
                string.Equals(capability, requestedCapability, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> ParseCapabilities(string capabilities)
        {
            return capabilities
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(capability => !string.IsNullOrWhiteSpace(capability))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public sealed record Response(
        IReadOnlyList<ModelItem> Models,
        int TotalModels);

    public sealed record ModelItem(
        Guid ModelId,
        string Name,
        string Provider,
        IReadOnlyList<string> Capabilities,
        string Status,
        int? ContextWindowSize);
}
