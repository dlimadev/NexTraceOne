using System.Text.RegularExpressions;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.ScaffoldServiceFromTemplate;

/// <summary>
/// Feature: ScaffoldServiceFromTemplate — instancia um novo serviço a partir de um template governado.
///
/// Fluxo de scaffolding:
///   1. Resolve o template por id ou slug
///   2. Valida que o template está ativo
///   3. Valida o nome do serviço (lowercase kebab-case)
///   4. Gera o manifesto de ficheiros substituindo variáveis ({{ServiceName}}, {{Domain}}, etc.)
///   5. Incrementa o contador de uso do template
///   6. Retorna o plano de scaffolding para o cliente executar (CLI/IDE Extension/CI)
///
/// Nota: a execução real dos ficheiros (git init, mkdir, etc.) é responsabilidade
/// do cliente que consome o payload. Esta feature apenas produz o plano estruturado.
///
/// Valor: developer recebe um plano completo de criação de serviço conforme com
/// contratos, ownership, políticas e estrutura — sem configuração manual.
/// Persona primária: Developer, Tech Lead.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ScaffoldServiceFromTemplate
{
    private static readonly Regex ServiceNamePattern =
        new(@"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$", RegexOptions.Compiled);

    /// <summary>Comando de scaffolding de serviço a partir de template.</summary>
    public sealed record Command(
        Guid? TemplateId,
        string? TemplateSlug,
        string ServiceName,
        string? TeamName = null,
        string? Domain = null,
        string? RepositoryUrl = null,
        IDictionary<string, string>? ExtraVariables = null) : ICommand<Response>;

    /// <summary>Valida o comando de scaffolding.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(c => c.TemplateId.HasValue || !string.IsNullOrWhiteSpace(c.TemplateSlug))
                .WithMessage("Either TemplateId or TemplateSlug must be provided.");

            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(64)
                .Must(n => ServiceNamePattern.IsMatch(n))
                .WithMessage("Service name must be lowercase kebab-case (e.g. 'payment-service').");

            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
        }
    }

    /// <summary>Handler que gera o plano de scaffolding para o serviço.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Resolver template
            var template = request.TemplateId.HasValue
                ? await repository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                : await repository.GetBySlugAsync(request.TemplateSlug!, cancellationToken);

            if (template is null)
            {
                return request.TemplateId.HasValue
                    ? ServiceTemplateErrors.NotFound(request.TemplateId.Value)
                    : ServiceTemplateErrors.NotFoundBySlug(request.TemplateSlug!);
            }

            if (!template.IsActive)
                return ServiceTemplateErrors.TemplateDisabled(template.Id.Value);

            // Resolver variáveis de substituição
            var variables = BuildVariables(request, template.DefaultDomain, template.DefaultTeam);
            if (request.ExtraVariables is not null)
            {
                foreach (var kv in request.ExtraVariables)
                    variables[kv.Key] = kv.Value;
            }

            // Gerar manifesto de ficheiros
            var files = GenerateScaffoldingFiles(template.ScaffoldingManifestJson, variables);

            // Incrementar uso do template
            template.IncrementUsage();
            await repository.UpdateAsync(template, cancellationToken);

            return Result<Response>.Success(new Response(
                ScaffoldingId: Guid.NewGuid(),
                ServiceName: request.ServiceName,
                TemplateId: template.Id.Value,
                TemplateSlug: template.Slug,
                TemplateVersion: template.Version,
                ServiceType: template.ServiceType.ToString(),
                Language: template.Language.ToString(),
                Domain: variables.GetValueOrDefault("Domain", template.DefaultDomain),
                TeamName: variables.GetValueOrDefault("TeamName", template.DefaultTeam),
                GovernancePolicyIds: template.GovernancePolicyIds,
                BaseContractSpec: ApplyVariables(template.BaseContractSpec, variables),
                Files: files,
                RepositoryUrl: request.RepositoryUrl,
                Variables: variables.AsReadOnly()));
        }

        private static Dictionary<string, string> BuildVariables(
            Command request,
            string defaultDomain,
            string defaultTeam)
        {
            var serviceName = request.ServiceName;
            var pascalCase = string.Concat(
                serviceName.Split('-').Select(p => p.Length > 0
                    ? char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..] : string.Empty)
                    : string.Empty));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ServiceName"] = serviceName,
                ["ServiceNamePascal"] = pascalCase,
                ["ServiceNameUpper"] = serviceName.ToUpperInvariant().Replace("-", "_"),
                ["Domain"] = request.Domain ?? defaultDomain,
                ["TeamName"] = request.TeamName ?? defaultTeam,
                ["Year"] = DateTimeOffset.UtcNow.Year.ToString(),
                ["Date"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")
            };
        }

        private static IReadOnlyList<ScaffoldedFile> GenerateScaffoldingFiles(
            string? manifestJson,
            Dictionary<string, string> variables)
        {
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                // Manifesto padrão mínimo: README + estrutura de pastas
                return new[]
                {
                    new ScaffoldedFile("README.md",
                        ApplyVariables("# {{ServiceName}}\n\nService scaffolded via NexTraceOne ServiceTemplate.", variables)),
                    new ScaffoldedFile(".nextraceone.json",
                        ApplyVariables(
                            """{"service":"{{ServiceName}}","domain":"{{Domain}}","team":"{{TeamName}}","scaffoldedAt":"{{Date}}"}""",
                            variables))
                };
            }

            // Se o manifesto existe, retornar as entradas com variáveis substituídas
            try
            {
                var entries = System.Text.Json.JsonSerializer.Deserialize<ManifestEntry[]>(manifestJson);
                if (entries is null)
                    return Array.Empty<ScaffoldedFile>();

                return entries
                    .Select(e => new ScaffoldedFile(
                        ApplyVariables(e.Path, variables),
                        ApplyVariables(e.Content, variables)))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<ScaffoldedFile>();
            }
        }

        private static string? ApplyVariables(string? template, Dictionary<string, string> variables)
        {
            if (template is null) return null;

            foreach (var kv in variables)
                template = template.Replace($"{{{{{kv.Key}}}}}", kv.Value, StringComparison.Ordinal);

            return template;
        }
    }

    /// <summary>Plano de scaffolding com ficheiros e contexto de criação do serviço.</summary>
    public sealed record Response(
        Guid ScaffoldingId,
        string ServiceName,
        Guid TemplateId,
        string TemplateSlug,
        string TemplateVersion,
        string ServiceType,
        string Language,
        string Domain,
        string TeamName,
        IReadOnlyList<Guid> GovernancePolicyIds,
        string? BaseContractSpec,
        IReadOnlyList<ScaffoldedFile> Files,
        string? RepositoryUrl,
        IReadOnlyDictionary<string, string> Variables);

    /// <summary>Ficheiro gerado pelo scaffolding.</summary>
    public sealed record ScaffoldedFile(string Path, string? Content);

    /// <summary>Entrada do manifesto de scaffolding (JSON interno).</summary>
    private sealed record ManifestEntry(string Path, string Content);
}
