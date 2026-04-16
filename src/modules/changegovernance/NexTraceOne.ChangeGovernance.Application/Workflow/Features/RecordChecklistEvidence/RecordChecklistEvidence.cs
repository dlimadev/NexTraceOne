using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.RecordChecklistEvidence;

/// <summary>
/// Feature: RecordChecklistEvidence — regista o resultado de uma execução de checklist
/// de release no EvidencePack de uma instância de workflow.
///
/// Liga o resultado da execução do checklist directamente ao pacote de evidências da release,
/// tornando-o rastreável e auditável. O checklist é serializado como JSON e armazenado
/// no campo ApprovalHistory do EvidencePack.
///
/// Fluxo:
///   1. Valida a instância de workflow e o EvidencePack associado.
///   2. Serializa os itens de checklist com estado (complete/incomplete) e notas.
///   3. Actualiza o campo ApprovalHistory do EvidencePack com os dados serializados.
///   4. Recalcula a completude do EvidencePack.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordChecklistEvidence
{
    /// <summary>Comando para registar a execução de checklist no EvidencePack.</summary>
    public sealed record Command(
        Guid WorkflowInstanceId,
        string ChecklistName,
        IReadOnlyList<ChecklistItemInput> Items,
        string ExecutedBy) : ICommand<Response>;

    /// <summary>Item individual do checklist com o seu estado de execução.</summary>
    public sealed record ChecklistItemInput(
        string Name,
        bool Completed,
        string? Notes);

    /// <summary>Valida o comando de registo de checklist.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
            RuleFor(x => x.ChecklistName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ExecutedBy).NotEmpty().MaximumLength(500);

            RuleFor(x => x.Items)
                .NotEmpty()
                .Must(items => items.Count <= 200)
                .WithMessage("A checklist cannot have more than 200 items.");

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.Name).NotEmpty().MaximumLength(1000);
                    item.RuleFor(i => i.Notes).MaximumLength(5000).When(i => i.Notes is not null);
                });
        }
    }

    /// <summary>Handler que serializa o checklist e o armazena no EvidencePack.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IWorkflowUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);

            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var pack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(instanceId, cancellationToken);
            if (pack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            var completedCount = request.Items.Count(i => i.Completed);
            var totalCount = request.Items.Count;
            var completionRate = totalCount > 0
                ? Math.Round((decimal)completedCount / totalCount * 100m, 2)
                : 0m;

            var checklistJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                checklistName = request.ChecklistName,
                executedBy = request.ExecutedBy,
                executedAt = dateTimeProvider.UtcNow,
                totalItems = totalCount,
                completedItems = completedCount,
                completionRate,
                items = request.Items.Select(i => new
                {
                    name = i.Name,
                    completed = i.Completed,
                    notes = i.Notes
                })
            });

            pack.SetApprovalHistory(checklistJson);
            evidencePackRepository.Update(pack);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                EvidencePackId: pack.Id.Value,
                WorkflowInstanceId: pack.WorkflowInstanceId.Value,
                ChecklistName: request.ChecklistName,
                TotalItems: totalCount,
                CompletedItems: completedCount,
                CompletionRate: completionRate,
                EvidenceCompletenessPercentage: pack.CompletenessPercentage,
                RecordedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta com o resultado do registo de checklist no EvidencePack.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        Guid WorkflowInstanceId,
        string ChecklistName,
        int TotalItems,
        int CompletedItems,
        decimal CompletionRate,
        decimal EvidenceCompletenessPercentage,
        DateTimeOffset RecordedAt);
}
