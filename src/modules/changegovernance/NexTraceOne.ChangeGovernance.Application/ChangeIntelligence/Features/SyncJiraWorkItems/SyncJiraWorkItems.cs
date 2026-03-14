using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.SyncJiraWorkItems;

/// <summary>
/// Feature: SyncJiraWorkItems — stub de sincronização com Jira (integração não configurada).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
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

    /// <summary>Handler stub de sincronização com Jira — integração não configurada neste ambiente.</summary>
    public sealed class Handler(IReleaseRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            return new Response(release.Id.Value, "Jira sync not configured. Configure the Jira integration to enable this feature.");
        }
    }

    /// <summary>Resposta do comando de sincronização com Jira.</summary>
    public sealed record Response(Guid ReleaseId, string Message);
}
