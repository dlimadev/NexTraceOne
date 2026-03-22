using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.AuditCompliance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.GetCompliancePolicy;

/// <summary>
/// Feature: GetCompliancePolicy — obtém uma política de compliance pelo identificador.
/// </summary>
public static class GetCompliancePolicy
{
    /// <summary>Query de obtenção de política de compliance.</summary>
    public sealed record Query(Guid PolicyId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém a política pelo Id.</summary>
    public sealed class Handler(ICompliancePolicyRepository compliancePolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = await compliancePolicyRepository.GetByIdAsync(
                CompliancePolicyId.From(request.PolicyId), cancellationToken);

            if (policy is null)
                return AuditErrors.CompliancePolicyNotFound(request.PolicyId);

            return new Response(
                policy.Id.Value,
                policy.Name,
                policy.DisplayName,
                policy.Description,
                policy.Category,
                policy.Severity,
                policy.IsActive,
                policy.EvaluationCriteria,
                policy.CreatedAt,
                policy.UpdatedAt);
        }
    }

    /// <summary>Resposta da obtenção de política de compliance.</summary>
    public sealed record Response(
        Guid PolicyId,
        string Name,
        string DisplayName,
        string? Description,
        string Category,
        ComplianceSeverity Severity,
        bool IsActive,
        string? EvaluationCriteria,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
