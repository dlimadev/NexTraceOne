using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.RecordBenchmarkConsent;

/// <summary>
/// Feature: RecordBenchmarkConsent — regista, concede ou revoga o consentimento de um tenant
/// para participação em benchmarks cross-tenant anonimizados (LGPD/GDPR opt-in).
/// Wave D.2 — Cross-tenant Benchmarks anonimizados.
/// </summary>
public static class RecordBenchmarkConsent
{
    /// <summary>Ação de consentimento a executar.</summary>
    public enum ConsentAction { Request, Grant, Revoke }

    public sealed record Command(
        string TenantId,
        ConsentAction Action,
        string? LgpdLawfulBasis = null,
        string? UserId = null) : ICommand<Response>;

    public sealed record Response(
        Guid ConsentId,
        string TenantId,
        BenchmarkConsentStatus Status,
        bool IsOptedIn);

    public sealed class Handler(
        ITenantBenchmarkConsentRepository consentRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var consent = await consentRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);

            if (request.Action == ConsentAction.Request)
            {
                var basis = request.LgpdLawfulBasis ?? "Legitimate interest — aggregated operational benchmarks";

                if (consent is null)
                {
                    consent = TenantBenchmarkConsent.RequestConsent(request.TenantId, basis);
                    consentRepository.Add(consent);
                }
            }
            else if (request.Action == ConsentAction.Grant)
            {
                if (consent is null)
                {
                    var basis = request.LgpdLawfulBasis ?? "Legitimate interest — aggregated operational benchmarks";
                    consent = TenantBenchmarkConsent.RequestConsent(request.TenantId, basis);
                    consentRepository.Add(consent);
                }

                consent.Grant(request.UserId, now);
                consentRepository.Update(consent);
            }
            else // Revoke
            {
                if (consent is null)
                    return Error.NotFound("Compliance.Benchmark.ConsentNotFound", "Benchmark consent for tenant '{0}' was not found.", request.TenantId);

                consent.Revoke(request.UserId, now);
                consentRepository.Update(consent);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                consent.Id.Value,
                consent.TenantId,
                consent.Status,
                consent.IsOptedIn));
        }
    }
}
