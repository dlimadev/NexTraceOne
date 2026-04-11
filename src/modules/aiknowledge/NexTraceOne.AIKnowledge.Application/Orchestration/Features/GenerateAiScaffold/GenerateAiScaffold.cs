using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Templates.ServiceInterfaces;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateAiScaffold;

/// <summary>
/// Feature: GenerateAiScaffold — gera o código completo de scaffolding de um novo serviço
/// assistido por IA, a partir de um template governado.
///
/// Fluxo:
///   1. Resolve o template pelo ID ou slug via ICatalogTemplatesModule (inter-module).
///   2. Constrói contexto rico: linguagem, tipo de serviço, manifesto de scaffolding,
///      contrato base e intenção do developer.
///   3. Invoca o IExternalAIRoutingPort com prompt estruturado.
///   4. A IA gera o código de cada ficheiro do projeto (controllers, DTOs, domain, tests, etc.).
///   5. Retorna a lista de ficheiros gerados prontos a fazer download ou registar no catálogo.
///
/// Diferença de ScaffoldServiceFromTemplate:
///   - ScaffoldServiceFromTemplate: substituição determinística de variáveis ({{ServiceName}}).
///   - GenerateAiScaffold: a IA gera o conteúdo real dos ficheiros com lógica de negócio,
///     rotas, request/response e testes baseados na intenção do developer.
///
/// Persona primária: Engineer, Tech Lead.
/// Pilar: AI-assisted Operations &amp; Engineering, Developer Acceleration.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GenerateAiScaffold
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>
    /// Comando para gerar scaffolding completo de um serviço com IA.
    /// O developer descreve o que o serviço deve fazer; a IA usa o template como guia estrutural.
    /// </summary>
    public sealed record Command(
        /// <summary>ID do template a usar como referência. Mutuamente exclusivo com TemplateSlug.</summary>
        Guid? TemplateId,
        /// <summary>Slug do template (alternativa ao ID).</summary>
        string? TemplateSlug,
        /// <summary>Nome do serviço a criar (kebab-case, ex: payment-api).</summary>
        string ServiceName,
        /// <summary>Descrição em linguagem natural do que o serviço deve fazer.</summary>
        string ServiceDescription,
        /// <summary>Equipa responsável (override do default do template).</summary>
        string? TeamName,
        /// <summary>Domínio de negócio (override do default do template).</summary>
        string? Domain,
        /// <summary>Linguagem/stack a usar (override do template; null = usar o do template).</summary>
        string? LanguageOverride,
        /// <summary>Entidades ou rotas principais que o serviço deve expor (ex: "Payment, Refund, Statement").</summary>
        string? MainEntities,
        /// <summary>Requisitos adicionais específicos para a geração.</summary>
        string? AdditionalRequirements,
        /// <summary>Provider de IA preferido (null = routing automático por política).</summary>
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly System.Text.RegularExpressions.Regex ServiceNamePattern =
            new(@"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>Supported language/stack identifiers for scaffold generation.</summary>
        private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "csharp", "c#", "dotnet", ".net",
            "java", "kotlin",
            "typescript", "javascript", "node", "nodejs",
            "python",
            "go", "golang",
            "rust",
            "ruby"
        };

        public Validator()
        {
            RuleFor(x => x).Must(x => x.TemplateId.HasValue || !string.IsNullOrWhiteSpace(x.TemplateSlug))
                .WithMessage("Either TemplateId or TemplateSlug must be provided.");

            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(64)
                .Must(n => ServiceNamePattern.IsMatch(n))
                .WithMessage("ServiceName must be lowercase kebab-case (e.g. 'payment-api').");

            RuleFor(x => x.ServiceDescription).NotEmpty().MaximumLength(3000);
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
            RuleFor(x => x.LanguageOverride)
                .Must(lang => SupportedLanguages.Contains(lang!))
                .WithMessage($"LanguageOverride must be one of: {string.Join(", ", SupportedLanguages.Order())}")
                .When(x => !string.IsNullOrWhiteSpace(x.LanguageOverride));
            RuleFor(x => x.MainEntities).MaximumLength(500).When(x => x.MainEntities is not null);
            RuleFor(x => x.AdditionalRequirements).MaximumLength(2000).When(x => x.AdditionalRequirements is not null);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        ICatalogTemplatesModule catalogTemplates,
        IExternalAIRoutingPort routingPort,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Resolver template via módulo Catalog
            var template = request.TemplateId.HasValue
                ? await catalogTemplates.GetActiveTemplateAsync(request.TemplateId.Value, cancellationToken)
                : await catalogTemplates.GetActiveTemplateBySlugAsync(request.TemplateSlug!, cancellationToken);

            if (template is null)
            {
                var identifier = request.TemplateId.HasValue
                    ? request.TemplateId.Value.ToString()
                    : request.TemplateSlug!;
                return Error.NotFound("GenerateAiScaffold.TemplateNotFound",
                    $"Active service template '{identifier}' was not found. Ensure the template exists and is active.");
            }

            var now = dateTimeProvider.UtcNow;
            var effectiveLanguage = request.LanguageOverride ?? template.Language;
            var effectiveDomain = request.Domain ?? template.DefaultDomain;
            var effectiveTeam = request.TeamName ?? template.DefaultTeam;

            // 2. Construir prompt rico com contexto do template e intenção do developer
            var prompt = BuildScaffoldPrompt(request, template, effectiveLanguage, effectiveDomain, effectiveTeam);
            var context = $"ai-scaffold-generation:{request.ServiceName}:{template.Slug}";

            string aiResponse;
            bool isFallback;

            try
            {
                aiResponse = await routingPort.RouteQueryAsync(
                    context,
                    prompt,
                    request.PreferredProvider,
                    capability: "scaffold-generation",
                    cancellationToken: cancellationToken);

                isFallback = aiResponse.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for GenerateAiScaffold. Service={ServiceName}, Template={TemplateSlug}",
                    request.ServiceName, template.Slug);
                return Error.Business("AIKnowledge.Provider.Unavailable",
                    "AI provider unavailable for scaffold generation: {0}", ex.Message);
            }

            // 3. Parsear resposta da IA em ficheiros estruturados
            var files = isFallback
                ? GenerateFallbackFiles(request.ServiceName, template, effectiveLanguage)
                : ParseAiResponseToFiles(aiResponse, request.ServiceName);

            logger.LogInformation(
                "AI scaffold generated for service '{ServiceName}' using template '{TemplateSlug}'. " +
                "Language={Language}, FileCount={FileCount}, IsFallback={IsFallback}, RequestedBy={UserId}",
                request.ServiceName, template.Slug, effectiveLanguage, files.Count, isFallback, currentUser.Id);

            return new Response(
                ScaffoldId: Guid.NewGuid(),
                ServiceName: request.ServiceName,
                TemplateId: template.TemplateId,
                TemplateSlug: template.Slug,
                Language: effectiveLanguage,
                ServiceType: template.ServiceType,
                Domain: effectiveDomain,
                TeamName: effectiveTeam,
                Files: files,
                IsFallback: isFallback,
                GeneratedAt: now);
        }

        private static string BuildScaffoldPrompt(
            Command request,
            ServiceTemplateSummary template,
            string language,
            string domain,
            string team)
        {
            var parts = new List<string>
            {
                $"You are an enterprise software architect. Generate a complete, production-ready project scaffold for a new service.",
                string.Empty,
                $"## Service Details",
                $"- Name: {request.ServiceName}",
                $"- Description: {request.ServiceDescription}",
                $"- Language/Stack: {language}",
                $"- Service Type: {template.ServiceType}",
                $"- Domain: {domain}",
                $"- Team: {team}"
            };

            if (!string.IsNullOrWhiteSpace(request.MainEntities))
                parts.Add($"- Main Entities/Routes: {request.MainEntities}");

            if (!string.IsNullOrWhiteSpace(request.AdditionalRequirements))
                parts.Add($"- Additional Requirements: {request.AdditionalRequirements}");

            parts.Add(string.Empty);
            parts.Add($"## Template: {template.DisplayName} v{template.Version}");
            parts.Add($"Architecture pattern: {template.ServiceType} with {language}");

            if (!string.IsNullOrWhiteSpace(template.ScaffoldingManifestJson))
            {
                parts.Add(string.Empty);
                parts.Add("## Required File Structure (follow this structure exactly):");
                parts.Add(template.ScaffoldingManifestJson);
            }

            if (!string.IsNullOrWhiteSpace(template.BaseContractSpec))
            {
                parts.Add(string.Empty);
                parts.Add("## Base Contract Spec (implement routes and models from this):");
                // Limit contract spec to avoid token overflow
                var spec = template.BaseContractSpec.Length > 4000
                    ? template.BaseContractSpec[..4000] + "\n[... truncated for brevity ...]"
                    : template.BaseContractSpec;
                parts.Add(spec);
            }

            parts.Add(string.Empty);
            parts.Add("## Output Requirements");
            parts.Add("Return the scaffold as a JSON array of file objects. Each object must have:");
            parts.Add("  - \"path\": relative file path (e.g. \"src/Controllers/PaymentController.cs\")");
            parts.Add("  - \"content\": complete file content as a string");
            parts.Add(string.Empty);
            parts.Add("Include:");
            parts.Add("  - All controllers/handlers with proper routes and HTTP methods");
            parts.Add("  - Request and Response DTOs/records");
            parts.Add("  - Domain entities and value objects when applicable");
            parts.Add("  - Service/use case layer when applicable");
            parts.Add("  - Dependency injection registration");
            parts.Add("  - Unit test skeleton for main operations");
            parts.Add("  - README.md with setup instructions");
            parts.Add("  - Project/build file (e.g. .csproj, pom.xml, package.json)");
            parts.Add(string.Empty);
            parts.Add("Apply enterprise best practices: proper error handling, structured logging placeholders, ");
            parts.Add("separation of concerns, and clear naming conventions. Use the description and entities to ");
            parts.Add("generate meaningful, non-placeholder code.");
            parts.Add(string.Empty);
            parts.Add("Respond ONLY with the JSON array. No explanation before or after.");

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Tenta parsear a resposta da IA (esperada como JSON array) em lista de ficheiros.
        /// Se o parse falhar, retorna um ficheiro único com o conteúdo bruto como README.
        /// </summary>
        private static IReadOnlyList<ScaffoldedFile> ParseAiResponseToFiles(string aiResponse, string serviceName)
        {
            try
            {
                // Extrair bloco JSON da resposta (a IA pode incluir marcadores de código)
                var json = aiResponse.Trim();
                if (json.StartsWith("```"))
                {
                    var start = json.IndexOf('[');
                    var end = json.LastIndexOf(']');
                    if (start >= 0 && end > start)
                        json = json[start..(end + 1)];
                }

                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<AiFileEntry>>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is { Count: > 0 })
                    return parsed.Select(f => new ScaffoldedFile(f.Path, f.Content)).ToList();
            }
            catch
            {
                // Falha silenciosa — retorna conteúdo bruto como README
            }

            return new List<ScaffoldedFile>
            {
                new($"SCAFFOLD_OUTPUT_{serviceName}.md", aiResponse)
            };
        }

        /// <summary>Ficheiros mínimos de fallback quando o provider de IA não está disponível.</summary>
        private static IReadOnlyList<ScaffoldedFile> GenerateFallbackFiles(
            string serviceName,
            ServiceTemplateSummary template,
            string language)
        {
            var pascal = ToPascalCase(serviceName);
            return new List<ScaffoldedFile>
            {
                new("README.md",
                    $"# {pascal}\n\n> Generated from template: {template.DisplayName} v{template.Version}\n\n" +
                    $"**Stack:** {language} | **Type:** {template.ServiceType}\n\n" +
                    "_AI provider was unavailable. This is a minimal scaffold. Re-generate when a provider is configured._"),
                new(".nextraceone.json",
                    $$$"""{"service": "{{{serviceName}}}", "template": "{{{template.Slug}}}", "version": "{{{template.Version}}}", "generatedBy": "fallback"}""")
            };
        }

        private static string ToPascalCase(string kebab)
            => string.Concat(kebab.Split('-').Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

        private sealed record AiFileEntry(string Path, string Content);
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Ficheiro gerado pelo AI scaffold.</summary>
    public sealed record ScaffoldedFile(string Path, string? Content);

    /// <summary>Resultado da geração de scaffolding com IA.</summary>
    public sealed record Response(
        Guid ScaffoldId,
        string ServiceName,
        Guid TemplateId,
        string TemplateSlug,
        string Language,
        string ServiceType,
        string Domain,
        string TeamName,
        IReadOnlyList<ScaffoldedFile> Files,
        bool IsFallback,
        DateTimeOffset GeneratedAt);
}
