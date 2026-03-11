using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.GetLicenseStatus;

/// <summary>
/// Feature: GetLicenseStatus — obtém o estado atual da licença.
/// </summary>
public static class GetLicenseStatus
{
    /// <summary>Query de status atual da licença.</summary>
    public sealed record Query(string LicenseKey) : IQuery<Response>;

    /// <summary>Valida a entrada da query de status.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que retorna o estado consolidado da licença.</summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(request.LicenseKey, cancellationToken);
            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName,
                license.IsActive,
                license.ExpiresAt,
                license.ExpiresAt <= dateTimeProvider.UtcNow,
                license.Capabilities.Count,
                license.UsageQuotas.Count,
                license.Activations.Count);
        }
    }

    /// <summary>Resposta consolidada do estado da licença.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName,
        bool IsActive,
        DateTimeOffset ExpiresAt,
        bool IsExpired,
        int CapabilityCount,
        int QuotaCount,
        int ActivationCount);
}
