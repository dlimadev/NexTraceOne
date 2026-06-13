using MediatR;

using NexTraceOne.Catalog.Contracts.Quality.ServiceInterfaces;

using EvaluateGatesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateTemplateQualityGates.EvaluateTemplateQualityGates;

namespace NexTraceOne.Catalog.Infrastructure.Quality.Services;

/// <summary>
/// Implementação do contrato inter-módulo ICatalogQualityGateModule.
/// Reutiliza a feature EvaluateTemplateQualityGates (via MediatR) para não duplicar a lógica
/// de avaliação, respeitando a fronteira de bounded context — o ChangeGovernance nunca acede
/// ao DbContext do Catalog.
/// </summary>
internal sealed class CatalogQualityGateModuleService(ISender sender) : ICatalogQualityGateModule
{
    /// <inheritdoc />
    public async Task<TemplateQualityGateResult> EvaluateAsync(
        string serviceId,
        string tenantId,
        Guid? templateId = null,
        string? templateSlug = null,
        CancellationToken ct = default)
    {
        var query = new EvaluateGatesFeature.Query(serviceId, tenantId, templateId, templateSlug);
        var result = await sender.Send(query, ct);

        if (result.IsFailure)
        {
            // Falha (ex: template indicado inexistente) — mapeada como gate não aprovado.
            return new TemplateQualityGateResult(
                ServiceId: serviceId,
                Status: "Error",
                Passed: false,
                RequiredCoverage: 0,
                ActualCoverage: null,
                SonarQualityGateStatus: null,
                Breaches: new[] { result.Error.Message });
        }

        var e = result.Value;
        return new TemplateQualityGateResult(
            ServiceId: e.ServiceId,
            Status: e.Status,
            Passed: e.Passed,
            RequiredCoverage: e.RequiredCoverage,
            ActualCoverage: e.ActualCoverage,
            SonarQualityGateStatus: e.SonarQualityGateStatus,
            Breaches: e.Breaches.Select(b => b.Detail).ToList());
    }
}
