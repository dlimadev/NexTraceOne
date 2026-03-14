using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.UpdateTelemetryConsent;

/// <summary>
/// Feature: UpdateTelemetryConsent — atualiza o consentimento de telemetria do tenant.
///
/// Regras de negócio:
/// - Se não existir registro, cria um novo TelemetryConsent associado à licença.
/// - Se já existir, atualiza o registro existente conforme a ação solicitada.
/// - Ações suportadas: grant (consentimento total), deny (revogar), partial (seletivo).
/// - Toda alteração é auditável: grava quem alterou, quando e o motivo.
/// - Respeita soberania de dados (LGPD/GDPR): cada tenant controla individualmente.
/// - O timestamp é obtido via IDateTimeProvider para testabilidade.
/// </summary>
public static class UpdateTelemetryConsent
{
    /// <summary>
    /// Comando para atualizar o consentimento de telemetria.
    /// Action: "grant" | "deny" | "partial"
    /// </summary>
    public sealed record Command(
        string LicenseKey,
        string Action,
        string UpdatedBy,
        string? Reason = null,
        bool AllowUsageMetrics = false,
        bool AllowPerformanceData = false,
        bool AllowErrorDiagnostics = false) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de consentimento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Action)
                .NotEmpty()
                .Must(a => a is "grant" or "deny" or "partial")
                .WithMessage("Action must be 'grant', 'deny', or 'partial'.");
            RuleFor(x => x.UpdatedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }

    /// <summary>
    /// Handler que cria ou atualiza o consentimento de telemetria.
    /// Usa IDateTimeProvider para obter o timestamp (nunca DateTimeOffset.UtcNow).
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(
                request.LicenseKey, cancellationToken);

            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            var now = dateTimeProvider.UtcNow;
            var consent = await licenseRepository.GetTelemetryConsentByLicenseIdAsync(
                license.Id, cancellationToken);

            if (consent is null)
            {
                consent = TelemetryConsent.Create(
                    license.Id,
                    TelemetryConsentStatus.NotRequested,
                    request.UpdatedBy,
                    now,
                    request.Reason);

                licenseRepository.AddTelemetryConsent(consent);
            }

            switch (request.Action)
            {
                case "grant":
                    consent.Grant(request.UpdatedBy, now, request.Reason);
                    break;
                case "deny":
                    consent.Deny(request.UpdatedBy, now, request.Reason);
                    break;
                case "partial":
                    consent.GrantPartial(
                        request.UpdatedBy,
                        now,
                        request.AllowUsageMetrics,
                        request.AllowPerformanceData,
                        request.AllowErrorDiagnostics,
                        request.Reason);
                    break;
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                consent.Status.ToString(),
                consent.AllowUsageMetrics,
                consent.AllowPerformanceData,
                consent.AllowErrorDiagnostics,
                consent.UpdatedBy,
                consent.UpdatedAt);
        }
    }

    /// <summary>Resposta com o novo estado do consentimento.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string Status,
        bool AllowUsageMetrics,
        bool AllowPerformanceData,
        bool AllowErrorDiagnostics,
        string UpdatedBy,
        DateTimeOffset UpdatedAt);
}
