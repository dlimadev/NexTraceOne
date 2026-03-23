using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação real do gerador de draft por IA.
/// Integra com IChatCompletionProvider e IAiModelCatalogService do módulo AIKnowledge.
/// Gera conteúdo real de contrato via IA governada, com fallback para null quando IA indisponível.
/// </summary>
public sealed class AiDraftGeneratorService : IAiDraftGenerator
{
    private readonly IChatCompletionProvider _chatProvider;
    private readonly IAiModelCatalogService _modelCatalog;
    private readonly ILogger<AiDraftGeneratorService> _logger;

    public AiDraftGeneratorService(
        IChatCompletionProvider chatProvider,
        IAiModelCatalogService modelCatalog,
        ILogger<AiDraftGeneratorService> logger)
    {
        _chatProvider = chatProvider;
        _modelCatalog = modelCatalog;
        _logger = logger;
    }

    public async Task<(string Content, string Format)?> GenerateAsync(
        ContractProtocol protocol,
        string title,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _modelCatalog.ResolveDefaultModelAsync("chat", cancellationToken);
            if (model is null)
            {
                _logger.LogWarning("No AI model available for contract draft generation — falling back to template");
                return null;
            }

            var (formatName, systemPrompt) = BuildPromptContext(protocol, title);

            var request = new ChatCompletionRequest(
                ModelId: model.ModelName,
                Messages: new[]
                {
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", prompt)
                },
                Temperature: 0.3,
                MaxTokens: 4000);

            var result = await _chatProvider.CompleteAsync(request, cancellationToken);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
            {
                _logger.LogWarning(
                    "AI draft generation failed: {Error} — falling back to template",
                    result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "AI draft generated successfully for '{Title}' using model {Model} ({Provider}). Tokens: {Prompt}+{Completion}",
                title, result.ModelId, result.ProviderId, result.PromptTokens, result.CompletionTokens);

            return (result.Content, formatName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI draft generation failed for '{Title}' — falling back to template", title);
            return null;
        }
    }

    private static (string Format, string SystemPrompt) BuildPromptContext(
        ContractProtocol protocol, string title)
    {
        return protocol switch
        {
            ContractProtocol.AsyncApi => ("yaml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid AsyncAPI 2.6.0 YAML contract for '{title}'.
                 Include channels, message schemas, and server bindings where applicable.
                 Output only the YAML content, no explanations.
                 """),

            ContractProtocol.Wsdl => ("xml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid WSDL 1.1 XML contract for '{title}'.
                 Include types, messages, portType, binding, and service elements.
                 Output only the XML content, no explanations.
                 """),

            _ => ("yaml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid OpenAPI 3.1.0 YAML contract for '{title}'.
                 Include paths, schemas, and response definitions.
                 Output only the YAML content, no explanations.
                 """)
        };
    }
}
