using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.RecordExternalInference;

/// <summary>
/// Feature: RecordExternalInference — regista uma inferência realizada por IA externa.
/// Garante auditabilidade e permite futura promoção para memória partilhada.
/// </summary>
public static class RecordExternalInference
{
    public sealed record Command(
        string ProviderId,
        string ModelName,
        string OriginalPrompt,
        string? AdditionalContext,
        string ResponseContent,
        string SensitivityClassification,
        int? QualityScore,
        bool CanPromoteToSharedMemory) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OriginalPrompt).NotEmpty().MaximumLength(32_000);
            RuleFor(x => x.ResponseContent).NotEmpty().MaximumLength(64_000);
            RuleFor(x => x.ProviderId).NotEmpty();
            RuleFor(x => x.ModelName).NotEmpty();
            RuleFor(x => x.SensitivityClassification).NotEmpty();
            RuleFor(x => x.QualityScore).InclusiveBetween(1, 5).When(x => x.QualityScore.HasValue);
        }
    }

    public sealed class Handler(
        IAiExternalInferenceRecordRepository inferenceRecordRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var record = AiExternalInferenceRecord.Create(
                userId: currentUser.IsAuthenticated ? currentUser.Id : "anonymous",
                tenantId: currentTenant.Id,
                providerId: request.ProviderId,
                modelName: request.ModelName,
                originalPrompt: request.OriginalPrompt,
                additionalContext: request.AdditionalContext,
                response: request.ResponseContent,
                sensitivityClassification: request.SensitivityClassification,
                qualityScore: request.QualityScore);

            await inferenceRecordRepository.AddAsync(record, cancellationToken);

            return new Response(record.Id.Value);
        }
    }

    public sealed record Response(Guid RecordId);
}
