using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de maturidade de serviços.
/// </summary>
public static class GovernanceMaturityErrors
{
    /// <summary>Avaliação de maturidade não encontrada pelo identificador.</summary>
    public static Error AssessmentNotFound(string id) => Error.NotFound(
        "Governance.ServiceMaturityAssessment.NotFound",
        "Service maturity assessment '{0}' was not found.", id);

    /// <summary>Nenhuma avaliação de maturidade encontrada para o serviço indicado.</summary>
    public static Error ServiceAssessmentNotFound(string serviceId) => Error.NotFound(
        "Governance.ServiceMaturityAssessment.ServiceNotFound",
        "No maturity assessment found for service '{0}'.", serviceId);

    /// <summary>Já existe uma avaliação de maturidade para o serviço indicado.</summary>
    public static Error AssessmentAlreadyExists(string serviceId) => Error.Conflict(
        "Governance.ServiceMaturityAssessment.AlreadyExists",
        "A maturity assessment already exists for service '{0}'.", serviceId);
}
