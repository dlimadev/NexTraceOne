using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação real do gerador de draft por IA.
/// Integra com IChatCompletionProvider e IAiModelCatalogService do módulo AIKnowledge.
/// Gera conteúdo real de contrato via IA governada, com fallback para null quando IA indisponível.
/// Verifica capability "ai_enabled" antes de qualquer chamada ao provider.
/// Regista uso de tokens via IAiTokenQuotaService para controlo de consumo externo.
/// </summary>
public sealed class AiDraftGeneratorService : IAiDraftGenerator
{
    private readonly IChatCompletionProvider _chatProvider;
    private readonly IAiModelCatalogService _modelCatalog;
    private readonly IAiTokenQuotaService _tokenQuotaService;
    private readonly IContextWindowManager _contextWindow;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AiDraftGeneratorService> _logger;

    public AiDraftGeneratorService(
        IChatCompletionProvider chatProvider,
        IAiModelCatalogService modelCatalog,
        IAiTokenQuotaService tokenQuotaService,
        IContextWindowManager contextWindow,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<AiDraftGeneratorService> logger)
    {
        _chatProvider = chatProvider;
        _modelCatalog = modelCatalog;
        _tokenQuotaService = tokenQuotaService;
        _contextWindow = contextWindow;
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
            var model = await _modelCatalog.ResolveModelForFeatureAsync(
                "catalog.contract-draft", "chat", _currentTenant.Id, cancellationToken);
            if (model is null)
            {
                _logger.LogWarning("No AI model available for contract draft generation — falling back to template");
                return null;
            }

            if (!model.IsInternal && !_currentTenant.HasCapability("ai_external"))
            {
                _logger.LogInformation(
                    "AI draft generation skipped — external AI is disabled for tenant {TenantId}", _currentTenant.Id);
                return null;
            }

            if (model.IsInternal && !_currentTenant.HasCapability("ai_internal"))
            {
                _logger.LogInformation(
                    "AI draft generation skipped — internal AI is disabled for tenant {TenantId}", _currentTenant.Id);
                return null;
            }

            var (formatName, systemPrompt) = BuildPromptContext(protocol, title);
            var estimatedTokens = _contextWindow.EstimateTokens(systemPrompt) + _contextWindow.EstimateTokens(prompt);

            var quotaResult = await _tokenQuotaService.ValidateQuotaAsync(
                _currentUser.Id,
                _currentTenant.Id,
                model.ProviderId,
                model.ModelName,
                estimatedTokens,
                cancellationToken);

            if (!quotaResult.IsAllowed)
            {
                _logger.LogWarning(
                    "AI draft generation blocked by token quota for tenant {TenantId}: {Reason}",
                    _currentTenant.Id, quotaResult.BlockReason);
                return null;
            }

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

            await _tokenQuotaService.RecordUsageAsync(
                _currentUser.Id,
                _currentTenant.Id,
                model.ProviderId,
                model.ModelName,
                model.ModelName,
                result.PromptTokens,
                result.CompletionTokens,
                requestId: Guid.NewGuid().ToString(),
                executionId: "catalog-draft",
                status: "success",
                durationMs: result.Duration.TotalMilliseconds,
                cancellationToken);

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
