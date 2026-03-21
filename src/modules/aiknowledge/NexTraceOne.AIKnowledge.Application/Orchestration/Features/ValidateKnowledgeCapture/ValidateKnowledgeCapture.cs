using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.ValidateKnowledgeCapture;

/// <summary>
/// Feature: ValidateKnowledgeCapture — valida se um capture de conhecimento da orquestração
/// está apto para ser aprovado e reutilizado. Verifica completude, origem, relevância,
/// duplicidade e adequação ao reaproveitamento. Retorna resultado estruturado auditável.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ValidateKnowledgeCapture
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para validar um capture de conhecimento de orquestração de IA.</summary>
    public sealed record Command(
        Guid EntryId) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EntryId).NotEmpty();
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IKnowledgeCaptureEntryRepository entryRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entry = await entryRepository.GetByIdAsync(
                KnowledgeCaptureEntryId.From(request.EntryId), cancellationToken);

            if (entry is null)
                return AiOrchestrationErrors.EntryNotFound(request.EntryId.ToString());

            var issues = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(entry.Title))
                issues.Add("Title is missing.");
            else if (entry.Title.Length < 10)
                issues.Add("Title is too short (minimum 10 characters).");

            if (string.IsNullOrWhiteSpace(entry.Content))
                issues.Add("Content is missing.");
            else if (entry.Content.Length < 50)
                issues.Add("Content is too short to be useful (minimum 50 characters).");
            else if (entry.Content.Length > 20_000)
                warnings.Add("Content is very long; consider summarising before approval.");

            if (string.IsNullOrWhiteSpace(entry.Source))
                issues.Add("Source identifier is missing.");

            if (entry.Relevance < 0.3m)
                issues.Add($"Relevance score ({entry.Relevance:F2}) is below the minimum threshold of 0.30.");
            else if (entry.Relevance < 0.5m)
                warnings.Add($"Relevance score ({entry.Relevance:F2}) is low; verify before approval.");

            if (entry.Status != Domain.Orchestration.Enums.KnowledgeEntryStatus.Suggested)
                issues.Add($"Entry has already been processed (Status={entry.Status}).");

            var duplicateExists = await entryRepository.HasDuplicateTitleInConversationAsync(
                entry.ConversationId,
                KnowledgeCaptureEntryId.From(request.EntryId),
                entry.Title,
                cancellationToken);

            if (duplicateExists)
                warnings.Add("Another entry with the same title exists in the same conversation. Verify for duplication.");

            var isValid = issues.Count == 0;

            var recommendation = isValid
                ? warnings.Count == 0
                    ? "Entry is complete and ready for approval."
                    : "Entry is valid but has warnings. Review before approving."
                : "Entry has issues that must be resolved before approval.";

            var normalizedMetadata = new Dictionary<string, string>
            {
                ["entryId"] = entry.Id.Value.ToString(),
                ["conversationId"] = entry.ConversationId.Value.ToString(),
                ["source"] = entry.Source,
                ["relevance"] = entry.Relevance.ToString("F2"),
                ["status"] = entry.Status.ToString(),
                ["suggestedAt"] = entry.SuggestedAt.ToString("O"),
                ["contentLength"] = entry.Content.Length.ToString()
            };

            return new Response(
                entry.Id.Value,
                isValid,
                issues,
                warnings,
                normalizedMetadata,
                recommendation,
                entry.Status.ToString());
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da validação de um capture de conhecimento.</summary>
    public sealed record Response(
        Guid EntryId,
        bool IsValid,
        IReadOnlyList<string> Issues,
        IReadOnlyList<string> Warnings,
        IReadOnlyDictionary<string, string> NormalizedMetadata,
        string Recommendation,
        string CurrentStatus);
}
