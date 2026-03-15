using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.GetIdeCapabilities;

/// <summary>
/// Feature: GetIdeCapabilities — obtém as capacidades disponíveis para um cliente IDE.
/// Retorna comandos, escopos, modelos permitidos e limites baseados no tipo de cliente e persona.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetIdeCapabilities
{
    /// <summary>Query de capacidades IDE para um tipo de cliente e persona.</summary>
    public sealed record Query(
        string ClientType,
        string? Persona) : IQuery<Response>;

    /// <summary>Handler que resolve capacidades IDE baseadas em política ou defaults.</summary>
    public sealed class Handler(
        IAiIdeCapabilityPolicyRepository capabilityPolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.ClientType);

            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;

            var policy = await capabilityPolicyRepository.GetByClientTypeAndPersonaAsync(
                clientType, request.Persona, cancellationToken);

            if (policy is not null && policy.IsActive)
            {
                return new Response(
                    clientType.ToString(),
                    request.Persona,
                    ParseCommaSeparated(policy.AllowedCommands),
                    ParseCommaSeparated(policy.AllowedContextScopes),
                    ParseCommaSeparated(policy.AllowedModelIds),
                    policy.AllowContractGeneration,
                    policy.AllowIncidentTroubleshooting,
                    policy.AllowExternalAI,
                    policy.MaxTokensPerRequest,
                    IsConfigured: true);
            }

            return new Response(
                clientType.ToString(),
                request.Persona,
                AllowedCommands: GetDefaultCommands(request.Persona),
                AllowedContextScopes: GetDefaultScopes(),
                AllowedModelIds: [],
                AllowContractGeneration: true,
                AllowIncidentTroubleshooting: true,
                AllowExternalAI: false,
                MaxTokensPerRequest: 4096,
                IsConfigured: false);
        }

        private static List<string> GetDefaultCommands(string? persona)
        {
            var commands = new List<string>
            {
                nameof(AIIDECommandType.Chat),
                nameof(AIIDECommandType.ServiceLookup),
                nameof(AIIDECommandType.ContractLookup),
                nameof(AIIDECommandType.SourceOfTruthQuery),
                nameof(AIIDECommandType.ServiceSummary)
            };

            if (persona is "Engineer" or "TechLead" or "Architect" or null)
            {
                commands.Add(nameof(AIIDECommandType.ContractGenerate));
                commands.Add(nameof(AIIDECommandType.ContractValidate));
                commands.Add(nameof(AIIDECommandType.IncidentLookup));
                commands.Add(nameof(AIIDECommandType.ChangeLookup));
                commands.Add(nameof(AIIDECommandType.RunbookLookup));
            }

            return commands;
        }

        private static List<string> GetDefaultScopes()
        {
            return ["services", "contracts", "incidents", "changes", "runbooks"];
        }

        private static List<string> ParseCommaSeparated(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return [];

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }

    /// <summary>Resposta com as capacidades IDE disponíveis.</summary>
    public sealed record Response(
        string ClientType,
        string? Persona,
        List<string> AllowedCommands,
        List<string> AllowedContextScopes,
        List<string> AllowedModelIds,
        bool AllowContractGeneration,
        bool AllowIncidentTroubleshooting,
        bool AllowExternalAI,
        int MaxTokensPerRequest,
        bool IsConfigured);
}
