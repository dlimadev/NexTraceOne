using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.SyncJiraWorkItems;

/// <summary>
/// Feature: SyncJiraWorkItems — sincronização de work items do Jira.
/// Integração formalmente diferida (PGLI) — retorna erro explícito quando invocada.
/// Decisão arquitetural: a integração Jira requer configuração de connector externo
/// e será disponibilizada como extensão via Integration Connectors do módulo Governance.
/// </summary>
public static class SyncJiraWorkItems
{
    /// <summary>Comando de sincronização de work items do Jira para uma Release.</summary>
    public sealed record Command(Guid ReleaseId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de sincronização com Jira.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna erro explícito indicando que a integração Jira
    /// está formalmente diferida e requer configuração de connector externo.
    /// </summary>
    public sealed class Handler(IReleaseRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            return Error.Validation(
                "JIRA_INTEGRATION_DEFERRED",
                "Jira work item sync is formally deferred (PGLI). Configure a Jira connector via Governance Integration Connectors to enable this capability.");
        }
    }

    /// <summary>Resposta do comando de sincronização com Jira.</summary>
    public sealed record Response(Guid ReleaseId, int SyncedItemCount);
}
