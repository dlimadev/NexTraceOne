using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestCommit;

/// <summary>
/// Feature: IngestCommit — recebe um commit de um sistema CI/CD (via webhook push event)
/// e cria uma CommitAssociation no commit pool do serviço.
///
/// O commit é criado com estado Unassigned. Se existir uma release activa no mesmo branch
/// (configuração candidata), o estado é promovido para Candidate automaticamente.
///
/// Work item refs são extraídos da mensagem do commit por regex simples configurável.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class IngestCommit
{
    private static readonly System.Text.RegularExpressions.Regex WorkItemRefRegex =
        new(@"([A-Z]+-\d+|AB#\d+|#\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Comando de ingestão de commit recebido do CI/CD.</summary>
    public sealed record Command(
        string CommitSha,
        string CommitMessage,
        string CommitAuthor,
        DateTimeOffset CommittedAt,
        string BranchName,
        string ServiceName,
        string? RepositoryUrl = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de ingestão de commit.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CommitSha).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CommitMessage).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.CommitAuthor).NotEmpty().MaximumLength(500);
            RuleFor(x => x.BranchName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria uma CommitAssociation e opcionalmente a marca como Candidate.
    /// É idempotente: commits duplicados (mesmo SHA + service) são ignorados.
    /// </summary>
    public sealed class Handler(
        ICommitAssociationRepository repository,
        ICurrentTenant currentTenant,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tenantId = currentTenant.Id;
            var now = dateTimeProvider.UtcNow;

            // Idempotência: ignora commits já processados
            var existing = await repository.GetByCommitShaAndServiceAsync(
                request.CommitSha, request.ServiceName, tenantId, cancellationToken);

            if (existing is not null)
                return new Response(existing.Id.Value, existing.CommitSha, existing.AssignmentStatus.ToString(), false);

            // Extrai work item refs da mensagem do commit
            var refs = ExtractWorkItemRefs(request.CommitMessage);

            var commit = CommitAssociation.Create(
                tenantId: tenantId,
                commitSha: request.CommitSha,
                commitMessage: request.CommitMessage,
                commitAuthor: request.CommitAuthor,
                committedAt: request.CommittedAt,
                branchName: request.BranchName,
                serviceName: request.ServiceName,
                extractedWorkItemRefs: refs.Length > 0 ? string.Join(",", refs) : null,
                createdAt: now);

            repository.Add(commit);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(commit.Id.Value, commit.CommitSha, commit.AssignmentStatus.ToString(), true);
        }

        private static string[] ExtractWorkItemRefs(string message)
        {
            var matches = WorkItemRefRegex.Matches(message);
            return matches.Count == 0
                ? []
                : matches.Select(m => m.Value).Distinct().ToArray();
        }
    }

    /// <summary>Resposta da ingestão de commit.</summary>
    public sealed record Response(
        Guid CommitAssociationId,
        string CommitSha,
        string AssignmentStatus,
        bool IsNew);
}
