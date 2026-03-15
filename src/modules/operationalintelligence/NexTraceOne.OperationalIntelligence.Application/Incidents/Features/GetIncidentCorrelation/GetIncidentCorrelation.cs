using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;

/// <summary>
/// Feature: GetIncidentCorrelation — retorna a correlação detalhada de um incidente.
/// Inclui mudanças relacionadas, confiança da correlação, serviços impactados,
/// dependências e contratos/eventos possivelmente afetados.
/// Base para futura IA operacional.
/// </summary>
public static class GetIncidentCorrelation
{
    /// <summary>Query para obter a correlação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe a correlação do incidente.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var correlation = FindCorrelation(request.IncidentId);
            if (correlation is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(correlation));
        }

        private static Response? FindCorrelation(string incidentId)
        {
            var now = DateTimeOffset.UtcNow;

            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Confidence: CorrelationConfidence.High,
                    Reason: "Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.",
                    RelatedChanges: new[]
                    {
                        new CorrelatedChange(Guid.NewGuid(), "Deploy v2.14.0 to Payment Gateway", "Deployment", "SuspectedRegression", now.AddHours(-4)),
                    },
                    RelatedServices: new[]
                    {
                        new CorrelatedService("svc-payment-gateway", "Payment Gateway", "Primary — source of degradation"),
                        new CorrelatedService("svc-order-api", "Order API", "Downstream — payment timeouts affecting orders"),
                    },
                    RelatedDependencies: new[]
                    {
                        new CorrelatedDependency("svc-order-api", "Order API", "Downstream consumer of payment service"),
                    },
                    PossibleImpactedContracts: new[]
                    {
                        new ImpactedContract(Guid.NewGuid(), "Payment Processing API", "v2.14.0", "REST"),
                    });
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Confidence: CorrelationConfidence.Low,
                    Reason: "No internal changes correlated. External dependency failure suspected.",
                    RelatedChanges: Array.Empty<CorrelatedChange>(),
                    RelatedServices: new[]
                    {
                        new CorrelatedService("svc-catalog-sync", "Catalog Sync", "Primary — integration component affected"),
                    },
                    RelatedDependencies: Array.Empty<CorrelatedDependency>(),
                    PossibleImpactedContracts: Array.Empty<ImpactedContract>());
            }

            return null;
        }
    }

    /// <summary>Resposta de correlação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        CorrelationConfidence Confidence,
        string Reason,
        IReadOnlyList<CorrelatedChange> RelatedChanges,
        IReadOnlyList<CorrelatedService> RelatedServices,
        IReadOnlyList<CorrelatedDependency> RelatedDependencies,
        IReadOnlyList<ImpactedContract> PossibleImpactedContracts);

    /// <summary>Mudança correlacionada.</summary>
    public sealed record CorrelatedChange(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    /// <summary>Serviço correlacionado.</summary>
    public sealed record CorrelatedService(
        string ServiceId, string DisplayName, string ImpactDescription);

    /// <summary>Dependência correlacionada.</summary>
    public sealed record CorrelatedDependency(
        string ServiceId, string DisplayName, string Relationship);

    /// <summary>Contrato possivelmente impactado.</summary>
    public sealed record ImpactedContract(
        Guid ContractVersionId, string Name, string Version, string Protocol);
}
