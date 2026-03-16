using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetCrossTeamDependencies;

/// <summary>
/// Feature: GetCrossTeamDependencies — dependências de entrada e saída entre equipas.
/// Permite visualizar o grafo de dependências ao nível da equipa para análise de blast radius e acoplamento.
/// </summary>
public static class GetCrossTeamDependencies
{
    /// <summary>Query para obter dependências cross-team de uma equipa pelo ID.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna dependências outbound e inbound da equipa.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var outbound = new List<OutboundDependencyDto>
            {
                new("Payment Gateway", "Identity Service", "team-identity", "Identity", "Synchronous"),
                new("Order API", "Notification Worker", "team-platform", "Platform", "Asynchronous")
            };

            var inbound = new List<InboundDependencyDto>
            {
                new("Pricing Engine", "Storefront BFF", "team-platform", "Platform", "Synchronous")
            };

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: "Commerce",
                Outbound: outbound,
                Inbound: inbound);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com dependências outbound e inbound da equipa.</summary>
    public sealed record Response(
        string TeamId,
        string TeamName,
        IReadOnlyList<OutboundDependencyDto> Outbound,
        IReadOnlyList<InboundDependencyDto> Inbound);

    /// <summary>DTO de dependência outbound — serviço da equipa que depende de outra equipa.</summary>
    public sealed record OutboundDependencyDto(
        string ServiceName,
        string TargetServiceName,
        string TargetTeamId,
        string TargetTeamName,
        string DependencyType);

    /// <summary>DTO de dependência inbound — serviço externo que depende de um serviço da equipa.</summary>
    public sealed record InboundDependencyDto(
        string ServiceName,
        string SourceServiceName,
        string SourceTeamId,
        string SourceTeamName,
        string DependencyType);
}
