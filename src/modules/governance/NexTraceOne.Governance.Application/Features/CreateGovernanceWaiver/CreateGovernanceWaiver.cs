using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreateGovernanceWaiver;

/// <summary>
/// Feature: CreateGovernanceWaiver — cria um pedido de exceção (waiver) para uma regra de governança.
/// Retorna o ID do waiver criado para acompanhamento do fluxo de aprovação.
/// </summary>
public static class CreateGovernanceWaiver
{
    /// <summary>Comando para criar um novo waiver de governança.</summary>
    public sealed record Command(
        string PackId,
        string? RuleId,
        string Scope,
        string ScopeType,
        string Justification,
        string RequestedBy,
        DateTimeOffset? ExpiresAt,
        IReadOnlyList<string> EvidenceLinks) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de waiver.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.RuleId).MaximumLength(200)
                .When(x => x.RuleId is not null);
            RuleFor(x => x.Scope).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ScopeType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Justification).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow)
                .When(x => x.ExpiresAt.HasValue)
                .WithMessage("ExpiresAt must be a future date.");
            RuleFor(x => x.EvidenceLinks).NotNull();
            RuleForEach(x => x.EvidenceLinks).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Handler que cria o waiver e retorna o ID gerado.</summary>
    public sealed class Handler(
        IGovernanceWaiverRepository waiverRepository,
        IGovernancePackRepository packRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Valida PackId
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var packId = new GovernancePackId(packGuid);
            var pack = await packRepository.GetByIdAsync(packId, cancellationToken);
            if (pack is null)
                return Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId);

            // Parse do ScopeType
            if (!Enum.TryParse<GovernanceScopeType>(request.ScopeType, ignoreCase: true, out var scopeType))
                return Error.Validation("INVALID_SCOPE_TYPE", "Scope type '{0}' is not valid.", request.ScopeType);

            var waiver = GovernanceWaiver.Create(
                packId: packId,
                ruleId: request.RuleId,
                scope: request.Scope,
                scopeType: scopeType,
                justification: request.Justification,
                requestedBy: request.RequestedBy,
                expiresAt: request.ExpiresAt,
                evidenceLinks: request.EvidenceLinks);

            await waiverRepository.AddAsync(waiver, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(WaiverId: waiver.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do waiver criado.</summary>
    public sealed record Response(string WaiverId);
}
