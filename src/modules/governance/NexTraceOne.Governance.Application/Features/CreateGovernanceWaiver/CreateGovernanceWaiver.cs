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
