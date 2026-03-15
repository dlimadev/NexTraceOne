using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.GetIdeSummary;

/// <summary>
/// Feature: GetIdeSummary — obtém resumo do estado das integrações IDE.
/// Retorna contagem de clientes registados, políticas configuradas e estado geral.
/// Utilizado pela interface administrativa para visão geral de IDE integrations.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetIdeSummary
{
    /// <summary>Query de resumo das integrações IDE.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que calcula o resumo das integrações IDE.</summary>
    public sealed class Handler(
        IAiIdeClientRegistrationRepository clientRegistrationRepository,
        IAiIdeCapabilityPolicyRepository capabilityPolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var vsCodeClients = await clientRegistrationRepository.ListAsync(
                null, AIClientType.VsCode, true, 1000, cancellationToken);
            var vsClients = await clientRegistrationRepository.ListAsync(
                null, AIClientType.VisualStudio, true, 1000, cancellationToken);
            var allPolicies = await capabilityPolicyRepository.ListAsync(
                null, null, 100, cancellationToken);

            var clientTypes = new List<ClientTypeSummary>
            {
                new(
                    AIClientType.VsCode.ToString(),
                    ActiveClients: vsCodeClients.Count,
                    HasCapabilityPolicy: allPolicies.Any(p => p.ClientType == AIClientType.VsCode && p.IsActive),
                    Status: vsCodeClients.Count > 0 ? "Active" : "Ready"),
                new(
                    AIClientType.VisualStudio.ToString(),
                    ActiveClients: vsClients.Count,
                    HasCapabilityPolicy: allPolicies.Any(p => p.ClientType == AIClientType.VisualStudio && p.IsActive),
                    Status: vsClients.Count > 0 ? "Active" : "Ready")
            };

            return new Response(
                clientTypes,
                TotalActiveClients: vsCodeClients.Count + vsClients.Count,
                TotalPolicies: allPolicies.Count,
                ActivePolicies: allPolicies.Count(p => p.IsActive));
        }
    }

    /// <summary>Resposta do resumo das integrações IDE.</summary>
    public sealed record Response(
        IReadOnlyList<ClientTypeSummary> ClientTypes,
        int TotalActiveClients,
        int TotalPolicies,
        int ActivePolicies);

    /// <summary>Resumo por tipo de cliente IDE.</summary>
    public sealed record ClientTypeSummary(
        string ClientType,
        int ActiveClients,
        bool HasCapabilityPolicy,
        string Status);
}
