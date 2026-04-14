using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.CreateCompliancePolicy;

/// <summary>
/// Feature: CreateCompliancePolicy — cria uma nova política de compliance.
/// </summary>
public static class CreateCompliancePolicy
{
    /// <summary>Comando de criação de política de compliance.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string Category,
        ComplianceSeverity Severity,
        string? EvaluationCriteria,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Severity).IsInEnum();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que cria a política e persiste no repositório.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository compliancePolicyRepository,
        IDateTimeProvider dateTimeProvider,
        IAuditComplianceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var policy = CompliancePolicy.Create(
                request.Name,
                request.DisplayName,
                request.Description,
                request.Category,
                request.Severity,
                request.EvaluationCriteria,
                request.TenantId,
                now);

            compliancePolicyRepository.Add(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(policy.Id.Value, policy.Name, policy.IsActive);
        }
    }

    /// <summary>Resposta da criação de política de compliance.</summary>
    public sealed record Response(Guid PolicyId, string Name, bool IsActive);
}
