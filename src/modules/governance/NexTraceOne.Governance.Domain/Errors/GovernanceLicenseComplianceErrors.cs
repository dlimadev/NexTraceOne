using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de compliance de licenças.
/// </summary>
public static class GovernanceLicenseComplianceErrors
{
    /// <summary>Relatório de compliance de licenças não encontrado pelo identificador.</summary>
    public static Error ReportNotFound(string id) => Error.NotFound(
        "Governance.LicenseCompliance.NotFound",
        "License compliance report '{0}' was not found.", id);
}
