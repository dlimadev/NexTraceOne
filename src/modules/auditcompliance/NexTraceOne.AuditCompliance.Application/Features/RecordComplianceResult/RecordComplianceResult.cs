using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.RecordComplianceResult;

/// <summary>
/// Feature: RecordComplianceResult — regista um resultado de avaliação de compliance.
/// </summary>
public static class RecordComplianceResult
{
    /// <summary>Comando de registo de resultado de compliance.</summary>
    public sealed record Command(
        Guid PolicyId,
        Guid? CampaignId,
        string ResourceType,
        string ResourceId,
        ComplianceOutcome Outcome,
        string? Details,
        string EvaluatedBy,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
            RuleFor(x => x.ResourceType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ResourceId).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Outcome).IsInEnum();
            RuleFor(x => x.EvaluatedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que regista o resultado de compliance.</summary>
    public sealed class Handler(
        IComplianceResultRepository complianceResultRepository,
        IDateTimeProvider dateTimeProvider,
        IAuditComplianceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var result = ComplianceResult.Create(
                CompliancePolicyId.From(request.PolicyId),
                request.CampaignId.HasValue ? AuditCampaignId.From(request.CampaignId.Value) : null,
                request.ResourceType,
                request.ResourceId,
                request.Outcome,
                request.Details,
                request.EvaluatedBy,
                now,
                request.TenantId);

            complianceResultRepository.Add(result);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(result.Id.Value, result.Outcome, result.EvaluatedAt);
        }
    }

    /// <summary>Resposta do registo de resultado de compliance.</summary>
    public sealed record Response(Guid ResultId, ComplianceOutcome Outcome, DateTimeOffset EvaluatedAt);
}
