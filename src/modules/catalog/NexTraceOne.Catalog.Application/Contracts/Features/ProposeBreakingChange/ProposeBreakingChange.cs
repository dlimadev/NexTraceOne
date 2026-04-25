using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ProposeBreakingChange;

/// <summary>
/// Feature: ProposeBreakingChange — cria uma proposta formal de breaking change num contrato
/// e inicia o workflow de consulta de consumidores.
///
/// Fluxo: cria proposta em estado Draft, abre consulta (ConsultationOpen),
/// e notifica consumidores activos via INotificationModule (se configurado).
///
/// Referência: CC-06, FUTURE-ROADMAP.md Wave A.2.
/// Ownership: módulo Catalog (Contracts).
/// </summary>
public static class ProposeBreakingChange
{
    public sealed record Command(
        string TenantId,
        Guid ContractId,
        string ProposedBreakingChangesJson,
        int MigrationWindowDays,
        string ProposedBy,
        bool OpenConsultationImmediately = true) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ContractId).NotEmpty();
            RuleFor(x => x.ProposedBreakingChangesJson).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.MigrationWindowDays).GreaterThan(0).LessThanOrEqualTo(365);
            RuleFor(x => x.ProposedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IBreakingChangeProposalRepository repository,
        IContractConsumerInventoryRepository consumerRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var proposal = BreakingChangeProposal.Create(
                tenantId: request.TenantId,
                contractId: request.ContractId,
                proposedBreakingChangesJson: request.ProposedBreakingChangesJson,
                migrationWindowDays: request.MigrationWindowDays,
                proposedBy: request.ProposedBy,
                utcNow: clock.UtcNow);

            if (request.OpenConsultationImmediately)
                proposal.OpenConsultation(clock.UtcNow);

            await repository.AddAsync(proposal, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var consumers = await consumerRepository.ListByContractAsync(
                request.ContractId, request.TenantId, cancellationToken);

            return Result<Response>.Success(new Response(
                ProposalId: proposal.Id.Value,
                ContractId: proposal.ContractId,
                Status: proposal.Status.ToString(),
                MigrationWindowDays: proposal.MigrationWindowDays,
                ActiveConsumersNotified: consumers.Count,
                ConsultationOpenedAt: proposal.ConsultationOpenedAt,
                CreatedAt: proposal.CreatedAt));
        }
    }

    public sealed record Response(
        Guid ProposalId,
        Guid ContractId,
        string Status,
        int MigrationWindowDays,
        int ActiveConsumersNotified,
        DateTimeOffset? ConsultationOpenedAt,
        DateTimeOffset CreatedAt);
}
