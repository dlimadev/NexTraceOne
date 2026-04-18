using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateMigrationPatch;

/// <summary>
/// Feature: GenerateMigrationPatch — gera sugestões de código para migrar implementações
/// quando um contrato muda entre versões.
/// Suporta geração para o lado provedor (server) e/ou consumidor (client).
/// Usa o diff semântico entre base e alvo para construir sugestões contextualizadas.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateMigrationPatch
{
    /// <summary>Alvo da geração do patch de migração.</summary>
    public enum PatchTarget { Provider, Consumer, All }

    /// <summary>Command para gerar patch de migração entre versões de contrato.</summary>
    public sealed record Command(
        Guid BaseVersionId,
        Guid TargetVersionId,
        PatchTarget Target,
        string? ImplementationLanguage) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do command de geração de patch.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BaseVersionId).NotEmpty();
            RuleFor(x => x.TargetVersionId).NotEmpty();
            RuleFor(x => x.BaseVersionId)
                .NotEqual(x => x.TargetVersionId)
                .WithMessage("Base and target versions must be different.");
        }
    }

    /// <summary>
    /// Handler que computa o diff entre versões de contrato e gera sugestões de código
    /// para o lado provedor e/ou consumidor, acelerando a adaptação após mudanças contratuais.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseVersion = await repository.GetByIdAsync(
                ContractVersionId.From(request.BaseVersionId), cancellationToken);
            if (baseVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.BaseVersionId.ToString());

            var targetVersion = await repository.GetByIdAsync(
                ContractVersionId.From(request.TargetVersionId), cancellationToken);
            if (targetVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.TargetVersionId.ToString());

            if (baseVersion.Protocol != targetVersion.Protocol)
                return ContractsErrors.ProtocolMismatchForDiff(
                    baseVersion.Protocol.ToString(),
                    targetVersion.Protocol.ToString());

            var diff = ContractDiffCalculator.ComputeDiff(
                baseVersion.SpecContent,
                targetVersion.SpecContent,
                targetVersion.Protocol);

            var language = string.IsNullOrWhiteSpace(request.ImplementationLanguage)
                ? "C#"
                : request.ImplementationLanguage;

            var providerSuggestions = (request.Target is PatchTarget.Provider or PatchTarget.All)
                ? BuildProviderSuggestions(diff, baseVersion, targetVersion, language)
                : [];

            var consumerSuggestions = (request.Target is PatchTarget.Consumer or PatchTarget.All)
                ? BuildConsumerSuggestions(diff, baseVersion, targetVersion, language)
                : [];

            return new Response(
                BaseVersionId: request.BaseVersionId,
                TargetVersionId: request.TargetVersionId,
                Protocol: baseVersion.Protocol.ToString(),
                Language: language,
                ChangeLevel: diff.ChangeLevel.ToString(),
                BreakingChangeCount: diff.BreakingChanges.Count,
                ProviderSuggestions: providerSuggestions,
                ConsumerSuggestions: consumerSuggestions,
                GeneratedAt: DateTime.UtcNow);
        }

        private static List<MigrationSuggestion> BuildProviderSuggestions(
            OpenApiDiffCalculator.DiffResult diff,
            ContractVersion baseVersion,
            ContractVersion targetVersion,
            string language)
        {
            var suggestions = new List<MigrationSuggestion>();

            foreach (var change in diff.BreakingChanges)
            {
                suggestions.Add(new MigrationSuggestion(
                    Kind: "breaking",
                    Side: "provider",
                    Description: $"Breaking change: {change.Description} [{change.ChangeType}] at {change.Path}",
                    CodeHint: BuildProviderBreakingHint(change, targetVersion, language),
                    Severity: "high"));
            }

            foreach (var change in diff.AdditiveChanges)
            {
                suggestions.Add(new MigrationSuggestion(
                    Kind: "additive",
                    Side: "provider",
                    Description: $"New capability to implement: {change.Description} [{change.ChangeType}] at {change.Path}",
                    CodeHint: BuildAdditiveProviderHint(change, targetVersion, language),
                    Severity: "medium"));
            }

            return suggestions;
        }

        private static List<MigrationSuggestion> BuildConsumerSuggestions(
            OpenApiDiffCalculator.DiffResult diff,
            ContractVersion baseVersion,
            ContractVersion targetVersion,
            string language)
        {
            var suggestions = new List<MigrationSuggestion>();

            foreach (var change in diff.BreakingChanges)
            {
                suggestions.Add(new MigrationSuggestion(
                    Kind: "breaking",
                    Side: "consumer",
                    Description: $"Consumer must adapt: {change.Description} [{change.ChangeType}] at {change.Path}",
                    CodeHint: BuildConsumerBreakingHint(change, baseVersion, targetVersion, language),
                    Severity: "high"));
            }

            foreach (var change in diff.NonBreakingChanges)
            {
                suggestions.Add(new MigrationSuggestion(
                    Kind: "non-breaking",
                    Side: "consumer",
                    Description: $"Optional consumer update: {change.Description} at {change.Path}",
                    CodeHint: null,
                    Severity: "low"));
            }

            return suggestions;
        }

        private static string BuildProviderBreakingHint(
            ChangeEntry change,
            ContractVersion targetVersion,
            string language)
        {
            return language switch
            {
                "TypeScript" or "JavaScript" =>
                    $"// Provider — Breaking change\n" +
                    $"// Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"// Change: {change.Description}\n" +
                    $"// TODO: Update the handler/controller for this route to match the new contract",
                "Java" =>
                    $"// Provider — Breaking change\n" +
                    $"// Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"// Change: {change.Description}\n" +
                    $"// TODO: Adjust @RequestMapping / response DTO to match v{targetVersion.SemVer}",
                "Python" =>
                    $"# Provider — Breaking change\n" +
                    $"# Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"# Change: {change.Description}\n" +
                    $"# TODO: Update route handler and Pydantic model for contract v{targetVersion.SemVer}",
                _ =>
                    $"// Provider — Breaking change\n" +
                    $"// Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"// Change: {change.Description}\n" +
                    $"// TODO: Review and update the controller/handler that implements this operation.\n" +
                    $"// Align response/request models with contract version {targetVersion.SemVer}."
            };
        }

        private static string BuildAdditiveProviderHint(
            ChangeEntry change,
            ContractVersion targetVersion,
            string language)
        {
            return language switch
            {
                "TypeScript" or "JavaScript" =>
                    $"// Provider — Additive change to implement\n" +
                    $"// Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"// Change: {change.Description}\n" +
                    $"// TODO: Add handler for this new endpoint or field",
                _ =>
                    $"// Provider — Additive change to implement\n" +
                    $"// Contract v{targetVersion.SemVer}: {change.ChangeType} at {change.Path}\n" +
                    $"// Change: {change.Description}\n" +
                    $"// TODO: Add controller action or DTO property for this additive change."
            };
        }

        private static string BuildConsumerBreakingHint(
            ChangeEntry change,
            ContractVersion baseVersion,
            ContractVersion targetVersion,
            string language)
        {
            return language switch
            {
                "TypeScript" or "JavaScript" =>
                    $"// Consumer — Breaking change migration\n" +
                    $"// Migrate from v{baseVersion.SemVer} to v{targetVersion.SemVer}\n" +
                    $"// Change: {change.Description} [{change.ChangeType}] at {change.Path}\n" +
                    $"// TODO: Update API call, adjust request/response type for new contract",
                "Java" =>
                    $"// Consumer — Breaking change migration\n" +
                    $"// Migrate from v{baseVersion.SemVer} to v{targetVersion.SemVer}\n" +
                    $"// Change: {change.Description} [{change.ChangeType}] at {change.Path}\n" +
                    $"// TODO: Update Feign/RestTemplate client, adjust DTO classes",
                "Python" =>
                    $"# Consumer — Breaking change migration\n" +
                    $"# Migrate from v{baseVersion.SemVer} to v{targetVersion.SemVer}\n" +
                    $"# Change: {change.Description} [{change.ChangeType}] at {change.Path}\n" +
                    $"# TODO: Update requests/httpx call and response model",
                _ =>
                    $"// Consumer — Breaking change migration\n" +
                    $"// Migrate from v{baseVersion.SemVer} to v{targetVersion.SemVer}\n" +
                    $"// Change: {change.Description} [{change.ChangeType}] at {change.Path}\n" +
                    $"// TODO: Update HttpClient call and/or DTO to match new contract shape."
            };
        }
    }

    /// <summary>Sugestão individual de migração de código.</summary>
    public sealed record MigrationSuggestion(
        string Kind,
        string Side,
        string Description,
        string? CodeHint,
        string Severity);

    /// <summary>Resposta do gerador de patch de migração de contrato.</summary>
    public sealed record Response(
        Guid BaseVersionId,
        Guid TargetVersionId,
        string Protocol,
        string Language,
        string ChangeLevel,
        int BreakingChangeCount,
        List<MigrationSuggestion> ProviderSuggestions,
        List<MigrationSuggestion> ConsumerSuggestions,
        DateTime GeneratedAt);
}
