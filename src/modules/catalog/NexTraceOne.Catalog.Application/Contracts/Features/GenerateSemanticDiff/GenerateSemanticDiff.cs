using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateSemanticDiff;

/// <summary>
/// Feature: GenerateSemanticDiff — cria um resultado de diff semântico assistido por IA
/// entre duas versões de contrato. Persiste o sumário em linguagem natural, classificação,
/// consumidores afetados, sugestões de mitigação e score de compatibilidade.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateSemanticDiff
{
    /// <summary>Comando para gerar um diff semântico assistido por IA entre duas versões de contrato.</summary>
    public sealed record Command(
        string ContractVersionFromId,
        string ContractVersionToId,
        string NaturalLanguageSummary,
        SemanticDiffClassification Classification,
        string? AffectedConsumers,
        string? MitigationSuggestions,
        int CompatibilityScore,
        string GeneratedByModel,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de geração de diff semântico.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionFromId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractVersionToId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.NaturalLanguageSummary).NotEmpty().MaximumLength(8000);
            RuleFor(x => x.Classification).IsInEnum();
            RuleFor(x => x.CompatibilityScore).InclusiveBetween(0, 100);
            RuleFor(x => x.GeneratedByModel).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria e persiste um resultado de diff semântico assistido por IA.
    /// Delega a criação ao factory method SemanticDiffResult.Generate.
    /// </summary>
    public sealed class Handler(
        ISemanticDiffResultRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = SemanticDiffResult.Generate(
                request.ContractVersionFromId,
                request.ContractVersionToId,
                request.NaturalLanguageSummary,
                request.Classification,
                request.AffectedConsumers,
                request.MitigationSuggestions,
                request.CompatibilityScore,
                request.GeneratedByModel,
                dateTimeProvider.UtcNow,
                request.TenantId);

            await repository.AddAsync(result, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                result.Id.Value,
                result.ContractVersionFromId,
                result.ContractVersionToId,
                result.NaturalLanguageSummary,
                result.Classification,
                result.CompatibilityScore,
                result.GeneratedByModel,
                result.GeneratedAt);
        }
    }

    /// <summary>Resposta da geração de diff semântico.</summary>
    public sealed record Response(
        Guid SemanticDiffResultId,
        string ContractVersionFromId,
        string ContractVersionToId,
        string NaturalLanguageSummary,
        SemanticDiffClassification Classification,
        int CompatibilityScore,
        string GeneratedByModel,
        DateTimeOffset GeneratedAt);
}
