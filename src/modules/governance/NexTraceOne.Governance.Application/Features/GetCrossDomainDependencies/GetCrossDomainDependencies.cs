using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetCrossDomainDependencies;

/// <summary>
/// Feature: GetCrossDomainDependencies — dependências de entrada e saída entre domínios de negócio.
/// Permite visualizar o grafo de dependências ao nível do domínio para análise de blast radius e acoplamento inter-domínio.
/// </summary>
public static class GetCrossDomainDependencies
{
    /// <summary>Query para obter dependências cross-domain de um domínio pelo ID.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna dependências outbound e inbound do domínio.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var outbound = new List<OutboundDomainDependencyDto>
            {
                new("Payment Gateway", "Commerce", "Identity Service", "domain-identity", "Identity", "Synchronous"),
                new("Order API", "Commerce", "Notification Worker", "domain-platform", "Platform", "Asynchronous")
            };

            var inbound = new List<InboundDomainDependencyDto>
            {
                new("Pricing Engine", "Commerce", "Analytics Aggregator", "domain-data", "Data & Analytics", "Asynchronous")
            };

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: "Commerce",
                Outbound: outbound,
                Inbound: inbound);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com dependências outbound e inbound do domínio.</summary>
    public sealed record Response(
        string DomainId,
        string DomainName,
        IReadOnlyList<OutboundDomainDependencyDto> Outbound,
        IReadOnlyList<InboundDomainDependencyDto> Inbound);

    /// <summary>DTO de dependência outbound — serviço do domínio que depende de outro domínio.</summary>
    public sealed record OutboundDomainDependencyDto(
        string ServiceName,
        string SourceDomainName,
        string TargetServiceName,
        string TargetDomainId,
        string TargetDomainName,
        string DependencyType);

    /// <summary>DTO de dependência inbound — serviço externo que depende de um serviço do domínio.</summary>
    public sealed record InboundDomainDependencyDto(
        string ServiceName,
        string TargetDomainName,
        string SourceServiceName,
        string SourceDomainId,
        string SourceDomainName,
        string DependencyType);
}
