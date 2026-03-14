using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.GenerateEvidencePack;

/// <summary>
/// Feature: GenerateEvidencePack — cria ou atualiza o evidence pack de uma instância de workflow.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateEvidencePack
{
    /// <summary>Comando para gerar ou atualizar o evidence pack de uma instância.</summary>
    public sealed record Command(
        Guid WorkflowInstanceId,
        string? ContractDiffSummary,
        decimal? BlastRadiusScore,
        decimal? SpectralScore,
        decimal? ChangeIntelligenceScore,
        string? ContractHash) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de geração de evidence pack.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
            RuleFor(x => x.ContractDiffSummary).MaximumLength(5000);
            RuleFor(x => x.BlastRadiusScore)
                .InclusiveBetween(0m, 1m)
                .When(x => x.BlastRadiusScore.HasValue);
            RuleFor(x => x.SpectralScore)
                .InclusiveBetween(0m, 1m)
                .When(x => x.SpectralScore.HasValue);
            RuleFor(x => x.ChangeIntelligenceScore)
                .InclusiveBetween(0m, 1m)
                .When(x => x.ChangeIntelligenceScore.HasValue);
            RuleFor(x => x.ContractHash).MaximumLength(256);
        }
    }

    /// <summary>Handler que cria ou atualiza o evidence pack e recalcula a completude.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);

            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var now = dateTimeProvider.UtcNow;

            var existingPack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(
                instanceId, cancellationToken);

            if (existingPack is not null)
            {
                existingPack.UpdateScores(
                    request.BlastRadiusScore,
                    request.SpectralScore,
                    request.ChangeIntelligenceScore);

                if (!string.IsNullOrWhiteSpace(request.ContractDiffSummary))
                    existingPack.SetContractDiff(request.ContractDiffSummary);

                if (!string.IsNullOrWhiteSpace(request.ContractHash))
                    existingPack.SetContractHash(request.ContractHash);

                evidencePackRepository.Update(existingPack);
                await unitOfWork.CommitAsync(cancellationToken);

                return new Response(
                    existingPack.Id.Value,
                    existingPack.CompletenessPercentage,
                    IsNew: false);
            }

            var pack = EvidencePack.Create(instanceId, instance.ReleaseId, now);

            pack.UpdateScores(
                request.BlastRadiusScore,
                request.SpectralScore,
                request.ChangeIntelligenceScore);

            if (!string.IsNullOrWhiteSpace(request.ContractDiffSummary))
                pack.SetContractDiff(request.ContractDiffSummary);

            if (!string.IsNullOrWhiteSpace(request.ContractHash))
                pack.SetContractHash(request.ContractHash);

            evidencePackRepository.Add(pack);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                pack.Id.Value,
                pack.CompletenessPercentage,
                IsNew: true);
        }
    }

    /// <summary>Resposta da geração do evidence pack.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        decimal CompletenessPercentage,
        bool IsNew);
}
