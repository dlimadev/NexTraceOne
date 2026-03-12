using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Application.Features.InstallDefaultRulesets;

/// <summary>
/// Feature: InstallDefaultRulesets -- cria rulesets padrão pre-instalados no sistema.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class InstallDefaultRulesets
{
    /// <summary>Comando de instalação de rulesets padrão.</summary>
    public sealed record Command() : ICommand<Response>;

    /// <summary>Valida a entrada do comando (sem parametros obrigatorios).</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() { }
    }

    /// <summary>Handler que cria o ruleset padrão "OpenAPI Best Practices".</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        private const string DefaultRulesetName = "OpenAPI Best Practices";
        private const string DefaultRulesetDescription = "Default ruleset with OpenAPI best practices for API governance.";
        private const string DefaultRulesetContent = """
            {
              "rules": {
                "operation-operationId": { "severity": "warn" },
                "operation-description": { "severity": "warn" },
                "info-contact": { "severity": "info" },
                "info-description": { "severity": "warn" },
                "no-eval-in-markdown": { "severity": "error" },
                "no-script-tags-in-markdown": { "severity": "error" },
                "path-params": { "severity": "error" },
                "operation-tags": { "severity": "warn" },
                "typed-enum": { "severity": "warn" }
              }
            }
            """;

        /// <summary>Processa o comando de instalação de rulesets padrão.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = Ruleset.Create(
                DefaultRulesetName,
                DefaultRulesetDescription,
                DefaultRulesetContent,
                RulesetType.Default,
                dateTimeProvider.UtcNow);

            repository.Add(ruleset);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(ruleset.Id.Value, ruleset.Name, ruleset.RulesetType.ToString());
        }
    }

    /// <summary>Resposta da instalação de rulesets padrão.</summary>
    public sealed record Response(Guid RulesetId, string Name, string RulesetType);
}
