using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.GetTelemetryConsent;

/// <summary>
/// Feature: GetTelemetryConsent — obtém o estado atual do consentimento de telemetria
/// associado à licença informada.
///
/// Regras de negócio:
/// - Cada licença possui no máximo um registro de TelemetryConsent.
/// - Se não existir registro, retorna status NotRequested com todas as flags desabilitadas.
/// - Respeita soberania de dados (LGPD/GDPR): cada tenant controla individualmente.
/// - O consentimento é independente do estado da licença (ativa/expirada).
/// </summary>
public static class GetTelemetryConsent
{
    /// <summary>Query para obter o consentimento de telemetria pela chave da licença.</summary>
    public sealed record Query(string LicenseKey) : IQuery<Response>;

    /// <summary>Valida a entrada da query de consentimento.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que retorna o estado do consentimento de telemetria.
    /// Se não existir registro, retorna valores padrão (NotRequested, tudo desabilitado).
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(request.LicenseKey, cancellationToken);
            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            var consent = await licenseRepository.GetTelemetryConsentByLicenseIdAsync(
                license.Id, cancellationToken);

            if (consent is null)
            {
                return new Response(
                    license.Id.Value,
                    license.LicenseKey,
                    "NotRequested",
                    false,
                    false,
                    false,
                    null,
                    null,
                    null);
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                consent.Status.ToString(),
                consent.AllowUsageMetrics,
                consent.AllowPerformanceData,
                consent.AllowErrorDiagnostics,
                consent.UpdatedBy,
                consent.UpdatedAt,
                consent.Reason);
        }
    }

    /// <summary>Resposta com o estado do consentimento de telemetria.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string Status,
        bool AllowUsageMetrics,
        bool AllowPerformanceData,
        bool AllowErrorDiagnostics,
        string? UpdatedBy,
        DateTimeOffset? UpdatedAt,
        string? Reason);
}
