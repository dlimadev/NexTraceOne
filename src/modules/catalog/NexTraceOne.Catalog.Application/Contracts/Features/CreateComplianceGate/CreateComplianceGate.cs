using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateComplianceGate;

/// <summary>
/// Feature: CreateComplianceGate — cria um gate de compliance contratual configurável.
/// Define regras, âmbito e comportamento de bloqueio para governança automatizada.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class CreateComplianceGate
{
    /// <summary>Comando para criar um novo gate de compliance contratual.</summary>
    public sealed record Command(
        string Name,
        string? Description,
        string? Rules,
        ComplianceGateScope Scope,
        string ScopeId,
        bool BlockOnViolation,
        string? CreatedBy,
        string? TenantId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de criação de gate de compliance.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.ScopeId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CreatedBy).MaximumLength(200).When(x => x.CreatedBy is not null);
        }
    }

    /// <summary>
    /// Handler que cria e persiste um novo gate de compliance contratual.
    /// </summary>
    public sealed class Handler(
        IContractComplianceGateRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gate = ContractComplianceGate.Create(
                name: request.Name,
                description: request.Description,
                rules: request.Rules,
                scope: request.Scope,
                scopeId: request.ScopeId,
                blockOnViolation: request.BlockOnViolation,
                createdBy: request.CreatedBy,
                createdAt: clock.UtcNow,
                tenantId: request.TenantId);

            await repository.AddAsync(gate, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                gate.Id.Value,
                gate.Name,
                gate.Scope,
                gate.ScopeId,
                gate.BlockOnViolation,
                gate.IsActive,
                gate.CreatedAt);
        }
    }

    /// <summary>Resposta da criação de gate de compliance contratual.</summary>
    public sealed record Response(
        Guid GateId,
        string Name,
        ComplianceGateScope Scope,
        string ScopeId,
        bool BlockOnViolation,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
