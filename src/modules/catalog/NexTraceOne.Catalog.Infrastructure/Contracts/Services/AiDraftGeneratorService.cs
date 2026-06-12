using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação real do gerador de draft por IA.
/// Integra com IAiExecutionGateway do módulo AIKnowledge.
/// Gera conteúdo real de contrato via IA governada, com fallback para null quando IA indisponível.
/// Verifica capability "ai_enabled" antes de qualquer chamada.
/// </summary>
public sealed class AiDraftGeneratorService : IAiDraftGenerator
{
    private readonly IAiExecutionGateway _aiExecutionGateway;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AiDraftGeneratorService> _logger;

    public AiDraftGeneratorService(
        IAiExecutionGateway aiExecutionGateway,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<AiDraftGeneratorService> logger)
    {
        _aiExecutionGateway = aiExecutionGateway;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<(string Content, string Format)?> GenerateAsync(
        ContractProtocol protocol,
        string title,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.HasCapability("ai_enabled"))
        {
            _logger.LogInformation("AI draft generation skipped — AI is disabled for tenant {TenantId}", _currentTenant.Id);
            return null;
        }

        try
        {
            var (formatName, systemPrompt) = BuildPromptContext(protocol, title);

            var result = await _aiExecutionGateway.ExecuteAsync(
                new AiExecutionRequest(
                    FeatureKey: "catalog.contract-draft",
                    RequestType: "chat",
                    UserPrompt: prompt,
                    SystemPrompt: systemPrompt,
                    Temperature: 0.3f,
                    MaxTokens: 4000),
                cancellationToken);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
            {
                _logger.LogWarning(
                    "AI draft generation failed: {Error} — falling back to template",
                    result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "AI draft generated successfully for '{Title}' using model {Model} ({Provider}). Tokens: {Prompt}+{Completion}",
                title, result.ResolvedModelId, result.ResolvedProviderId, result.PromptTokens, result.CompletionTokens);

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
        // Sanitize title to prevent prompt injection — strip control characters and limit length
        var safeTitle = new string(title
            .Where(c => !char.IsControl(c))
            .Take(200)
            .ToArray())
            .Replace("```", "")
            .Replace("###", "");

        return protocol switch
        {
            ContractProtocol.AsyncApi => ("yaml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid AsyncAPI 2.6.0 YAML contract for '{safeTitle}'.
                 Include channels, message schemas, and server bindings where applicable.
                 Output only the YAML content, no explanations.
                 """),

            ContractProtocol.Wsdl => ("xml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid WSDL 1.1 XML contract for '{safeTitle}'.
                 Include types, messages, portType, binding, and service elements.
                 Output only the XML content, no explanations.
                 """),

            _ => ("yaml",
                $"""
                 You are a contract generation assistant for NexTraceOne.
                 Generate a valid OpenAPI 3.1.0 YAML contract for '{safeTitle}'.
                 Include paths, schemas, and response definitions.
                 Output only the YAML content, no explanations.
                 """)
        };
    }
}
