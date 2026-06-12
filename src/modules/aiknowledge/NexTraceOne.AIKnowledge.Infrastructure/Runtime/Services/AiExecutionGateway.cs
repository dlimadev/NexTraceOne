using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Null;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do gateway central de execução de IA.
/// Resolve provider, modelo e governança de forma centralizada,
/// respeitando a hierarquia: user preference → tenant binding → routing strategy → system default.
/// 
/// Fase 4: Adiciona runtime fallback inteligente (tenta providers alternativos saudáveis
/// quando o primário falha), health check monitor para decisões rápidas,
/// e ranking por latência para seleção do melhor fallback.
/// </summary>
public sealed class AiExecutionGateway : IAiExecutionGateway
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly IAiModelCatalogService _modelCatalogService;
    private readonly IUserAiPreferenceRepository _userPreferenceRepository;
    private readonly IAiFeatureModelBindingRepository _featureBindingRepository;
    private readonly IAiModelAuthorizationService _authorizationService;
    private readonly IAiTokenQuotaService _tokenQuotaService;
    private readonly IAiGuardrailEnforcementService _guardrailService;
    private readonly IAiProviderHealthMonitor _healthMonitor;
    private readonly IAiRoutingMetricsService _routingMetrics;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<AiExecutionGateway> _logger;

    public AiExecutionGateway(
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalogService,
        IUserAiPreferenceRepository userPreferenceRepository,
        IAiFeatureModelBindingRepository featureBindingRepository,
        IAiModelAuthorizationService authorizationService,
        IAiTokenQuotaService tokenQuotaService,
        IAiGuardrailEnforcementService guardrailService,
        IAiProviderHealthMonitor healthMonitor,
        IAiRoutingMetricsService routingMetrics,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ILogger<AiExecutionGateway> logger)
    {
        _providerFactory = providerFactory;
        _modelCatalogService = modelCatalogService;
        _userPreferenceRepository = userPreferenceRepository;
        _featureBindingRepository = featureBindingRepository;
        _authorizationService = authorizationService;
        _tokenQuotaService = tokenQuotaService;
        _guardrailService = guardrailService;
        _healthMonitor = healthMonitor;
        _routingMetrics = routingMetrics;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<AiExecutionResult> ExecuteAsync(
        AiExecutionRequest request,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return BuildErrorResult(
                    AiAvailabilityStatus.NoProviderAvailable,
                    "Identificador de usuário inválido.",
                    stopwatch.Elapsed);
            }
            var tenantId = _currentTenant.Id;

            // 1. Resolve provider e modelo
            var resolution = await ResolveExecutionAsync(request, userId, tenantId, ct);
            if (!resolution.IsAvailable)
            {
                return BuildErrorResult(
                    resolution.Status,
                    resolution.UnavailabilityReason ?? "IA indisponível.",
                    stopwatch.Elapsed);
            }

            // 2. Guardrails — input
            if (!string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                var inputGuard = await _guardrailService.EvaluateInputAsync(
                    request.UserPrompt, tenantId, _currentUser.Persona ?? string.Empty, ct);
                if (inputGuard.IsBlocked)
                {
                    return BuildErrorResult(
                        AiAvailabilityStatus.GuardrailBlocked,
                        inputGuard.UserMessage ?? "Conteúdo bloqueado pelos guardrails de IA.",
                        stopwatch.Elapsed);
                }
            }

            // 3. Quota check (pré-execução estimada)
            var estimatedTokens = EstimateTokens(request);
            var quotaResult = await _tokenQuotaService.ValidateQuotaAsync(
                _currentUser.Id, tenantId, resolution.ProviderId, resolution.ModelId, estimatedTokens, ct);
            if (!quotaResult.IsAllowed)
            {
                return BuildErrorResult(
                    AiAvailabilityStatus.QuotaExceeded,
                    quotaResult.BlockReason ?? "Quota de tokens excedida.",
                    stopwatch.Elapsed);
            }

            // 4. Executa inferência com runtime fallback
            var (result, wasRuntimeFallback) = await ExecuteWithFallbackAsync(
                resolution, request, estimatedTokens, stopwatch, ct);

            if (!result.Success)
            {
                return BuildErrorResult(
                    AiAvailabilityStatus.NoProviderAvailable,
                    result.ErrorMessage ?? "Falha na execução de IA.",
                    stopwatch.Elapsed);
            }

            stopwatch.Stop();

            // 5. Guardrails — output
            if (!string.IsNullOrWhiteSpace(result.Content))
            {
                var outputGuard = await _guardrailService.EvaluateOutputAsync(
                    result.Content, tenantId, ct);
                if (outputGuard.IsBlocked)
                {
                    return BuildErrorResult(
                        AiAvailabilityStatus.GuardrailBlocked,
                        outputGuard.UserMessage ?? "Resposta bloqueada pelos guardrails de IA.",
                        stopwatch.Elapsed);
                }
            }

            // 6. Registra uso (fire-and-forget simplificado)
            var recordedUserId = _currentUser.Id;
            var recordedTenantId = tenantId;
            var recordedProviderId = result.ProviderId ?? resolution.ProviderId;
            var recordedModelId = result.ModelId ?? resolution.ModelId;
            var recordedModelDisplayName = resolution.ModelDisplayName;
            var recordedPromptTokens = result.PromptTokens;
            var recordedCompletionTokens = result.CompletionTokens;
            var recordedDurationMs = stopwatch.ElapsedMilliseconds;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _tokenQuotaService.RecordUsageAsync(
                        recordedUserId, recordedTenantId, recordedProviderId, recordedModelId, recordedModelDisplayName,
                        recordedPromptTokens, recordedCompletionTokens,
                        requestId: Guid.NewGuid().ToString(),
                        executionId: Guid.NewGuid().ToString(),
                        status: "success",
                        durationMs: recordedDurationMs,
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao registrar uso de tokens após execução de IA.");
                }
            }, ct);

            return new AiExecutionResult(
                Success: result.Success,
                Content: result.Content,
                ProviderType: resolution.ProviderType,
                ResolvedProviderId: result.ProviderId ?? resolution.ProviderId,
                ResolvedModelId: result.ModelId ?? resolution.ModelId,
                ResolvedModelDisplayName: resolution.ModelDisplayName,
                PromptTokens: result.PromptTokens,
                CompletionTokens: result.CompletionTokens,
                Duration: stopwatch.Elapsed,
                RoutingDecisionId: null,
                WasFallbackUsed: resolution.WasFallbackUsed || wasRuntimeFallback,
                ErrorMessage: result.ErrorMessage);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro inesperado no AI Execution Gateway para feature '{FeatureKey}'", request.FeatureKey);
            return BuildErrorResult(
                AiAvailabilityStatus.NoProviderAvailable,
                "Erro interno ao processar requisição de IA.",
                stopwatch.Elapsed);
        }
    }

    public async IAsyncEnumerable<AiExecutionStreamChunk> ExecuteStreamingAsync(
        AiExecutionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            yield return new AiExecutionStreamChunk(
                Content: null,
                IsComplete: true,
                ProviderType: AiProviderType.Null,
                ResolvedProviderId: "null",
                ResolvedModelId: "null",
                ErrorMessage: "Identificador de usuário inválido.");
            yield break;
        }
        var tenantId = _currentTenant.Id;

        var resolution = await ResolveExecutionAsync(request, userId, tenantId, ct);
        if (!resolution.IsAvailable)
        {
            yield return new AiExecutionStreamChunk(
                Content: null,
                IsComplete: true,
                ProviderType: resolution.ProviderType,
                ResolvedProviderId: resolution.ProviderId,
                ResolvedModelId: resolution.ModelId,
                ErrorMessage: resolution.UnavailabilityReason ?? "IA indisponível.");
            yield break;
        }

        var provider = resolution.Provider;
        if (provider is null)
        {
            yield return new AiExecutionStreamChunk(
                Content: null,
                IsComplete: true,
                ProviderType: resolution.ProviderType,
                ResolvedProviderId: resolution.ProviderId,
                ResolvedModelId: resolution.ModelId,
                ErrorMessage: "Provider resolvido está indisponível.");
            yield break;
        }

        var chatRequest = new ChatCompletionRequest(
            ModelId: resolution.ModelId,
            Messages: request.Messages ?? new List<ChatMessage>(),
            Temperature: request.Temperature ?? 0.7f,
            MaxTokens: request.MaxTokens ?? 4096,
            SystemPrompt: request.SystemPrompt);

        if (!string.IsNullOrWhiteSpace(request.UserPrompt) && (request.Messages == null || request.Messages.Count == 0))
        {
            chatRequest = chatRequest with
            {
                Messages = new List<ChatMessage> { new("user", request.UserPrompt) }
            };
        }

        // Note: Exception handling in async iterators with yield is limited in C#.
        // Callers should wrap the enumeration in their own try/catch for robust error handling.
        await foreach (var chunk in provider.CompleteStreamingAsync(chatRequest, ct))
        {
            yield return new AiExecutionStreamChunk(
                Content: chunk.Content,
                IsComplete: chunk.IsComplete,
                ProviderType: resolution.ProviderType,
                ResolvedProviderId: resolution.ProviderId,
                ResolvedModelId: resolution.ModelId,
                PromptTokens: chunk.PromptTokens,
                CompletionTokens: chunk.CompletionTokens,
                ErrorMessage: chunk.ErrorMessage);
        }
    }

    public async Task<AiExecutionPlan> PreviewExecutionAsync(
        AiExecutionRequest request,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return new AiExecutionPlan(
                ProviderType: AiProviderType.Null,
                ProviderId: "null",
                ModelId: "null",
                ModelDisplayName: "IA Desabilitada",
                IsAvailable: false,
                UnavailabilityReason: "Identificador de usuário inválido.",
                EstimatedCost: null,
                AppliedPolicies: Array.Empty<string>());
        }
        var tenantId = _currentTenant.Id;

        var resolution = await ResolveExecutionAsync(request, userId, tenantId, ct);

        return new AiExecutionPlan(
            ProviderType: resolution.ProviderType,
            ProviderId: resolution.ProviderId,
            ModelId: resolution.ModelId,
            ModelDisplayName: resolution.ModelDisplayName,
            IsAvailable: resolution.IsAvailable,
            UnavailabilityReason: resolution.UnavailabilityReason,
            EstimatedCost: null,
            AppliedPolicies: resolution.AppliedPolicies);
    }

    public async Task<AiAvailabilityStatus> CheckAvailabilityAsync(
        string featureKey,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return AiAvailabilityStatus.NoProviderAvailable;

        var tenantId = _currentTenant.Id;

        var resolution = await ResolveExecutionAsync(
            new AiExecutionRequest(FeatureKey: featureKey, RequestType: "chat"),
            userId, tenantId, ct);

        return resolution.Status;
    }

    // ── Internal execution with runtime fallback ─────────────────────────

    private async Task<(ChatCompletionResult Result, bool WasRuntimeFallback)> ExecuteWithFallbackAsync(
        ResolvedExecution primary,
        AiExecutionRequest request,
        int estimatedTokens,
        System.Diagnostics.Stopwatch stopwatch,
        CancellationToken ct)
    {
        var chatRequest = BuildChatRequest(request, primary.ModelId);
        var providersToTry = new List<(IChatCompletionProvider Provider, string ProviderId, string ModelId)>();

        if (primary.Provider is not null)
        {
            providersToTry.Add((primary.Provider, primary.ProviderId, primary.ModelId));
        }

        // Build fallback candidates: healthy providers that are different from primary
        var fallbackCandidates = await BuildFallbackCandidatesAsync(primary.ProviderId, primary.ModelId, ct);
        providersToTry.AddRange(fallbackCandidates);

        foreach (var (provider, providerId, modelId) in providersToTry)
        {
            try
            {
                _logger.LogDebug(
                    "Attempting execution with provider '{ProviderId}' model '{ModelId}' for feature '{FeatureKey}'",
                    providerId, modelId, request.FeatureKey);

                // Per-provider timeout: 60s attempt, linked to user cancellation token
                using var attemptCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, attemptCts.Token);

                var result = await provider.CompleteAsync(chatRequest with { ModelId = modelId }, linkedCts.Token);

                if (result is null)
                {
                    _logger.LogWarning(
                        "Provider '{ProviderId}' returned null result for feature '{FeatureKey}'",
                        providerId, request.FeatureKey);
                    continue;
                }

                if (result.Success)
                {
                    var wasFallback = !string.Equals(providerId, primary.ProviderId, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(modelId, primary.ModelId, StringComparison.OrdinalIgnoreCase);

                    if (wasFallback)
                    {
                        _logger.LogInformation(
                            "Runtime fallback succeeded: primary={PrimaryProvider} → fallback={FallbackProvider} for feature '{FeatureKey}'",
                            primary.ProviderId, providerId, request.FeatureKey);
                    }

                    return (result with { ProviderId = providerId, ModelId = modelId }, wasFallback);
                }

                _logger.LogWarning(
                    "Provider '{ProviderId}' returned unsuccessful result for feature '{FeatureKey}': {Error}",
                    providerId, request.FeatureKey, result.ErrorMessage);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // User cancelled — propagate immediately, don't try fallbacks
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Provider '{ProviderId}' failed for feature '{FeatureKey}'. Trying next candidate.",
                    providerId, request.FeatureKey);
            }
        }

        return (new ChatCompletionResult(
            Success: false,
            Content: null,
            ModelId: primary.ModelId,
            ProviderId: primary.ProviderId,
            PromptTokens: 0,
            CompletionTokens: 0,
            Duration: TimeSpan.Zero,
            ErrorMessage: "Todos os providers disponíveis falharam na execução."), false);
    }

    private async Task<IReadOnlyList<(IChatCompletionProvider Provider, string ProviderId, string ModelId)>> BuildFallbackCandidatesAsync(
        string excludeProviderId,
        string excludeModelId,
        CancellationToken ct)
    {
        var candidates = new List<(IChatCompletionProvider Provider, string ProviderId, string ModelId)>();

        var healthyIds = _healthMonitor.GetHealthyProviderIds()
            .Where(id => !string.Equals(id, excludeProviderId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (healthyIds.Count == 0)
            return candidates;

        // Rank by latency (if metrics available)
        var rankings = await _routingMetrics.RankProvidersByLatencyAsync(healthyIds, ct);
        var rankedIds = rankings.Select(r => r.ProviderId).ToList();

        // Include any healthy providers not in ranking at the end
        foreach (var id in healthyIds)
        {
            if (!rankedIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                rankedIds.Add(id);
        }

        // Resolve default model once (avoids N+1 query)
        var defaultModel = await _modelCatalogService.ResolveDefaultModelAsync("chat", ct);

        foreach (var providerId in rankedIds)
        {
            var provider = _providerFactory.GetChatProvider(providerId);
            if (provider is null)
                continue;

            // Try to resolve a model for this provider
            var modelId = defaultModel?.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase) == true
                ? defaultModel.ModelName
                : (await InferModelForProviderAsync(providerId, ct));

            // Skip exact same provider+model pair being excluded
            if (string.Equals(providerId, excludeProviderId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(modelId, excludeModelId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(modelId))
            {
                candidates.Add((provider, providerId, modelId));
            }
        }

        return candidates;
    }

    private async Task<string?> InferModelForProviderAsync(string providerId, CancellationToken ct)
    {
        // Query catalog for any active chat model belonging to this provider
        // If catalog supports it, prefer catalog-backed lookup over hardcoded fallback
        var defaultModel = await _modelCatalogService.ResolveDefaultModelAsync("chat", ct);
        if (defaultModel is not null &&
            string.Equals(defaultModel.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
        {
            return defaultModel.ModelName;
        }

        // Hardcoded fallback mapping — last resort when catalog doesn't have a match.
        // These are validated defaults that should exist in DefaultModelCatalog seed data.
        return providerId.ToLowerInvariant() switch
        {
            "ollama" => "llama3.2",
            "openai" => "gpt-4o-mini",
            "anthropic" => "claude-3-5-sonnet",
            "gemini" => "gemini-1.5-pro",
            "github-copilot" => "gpt-4o",
            "lmstudio" => "local-model",
            _ => null
        };
    }

    private static ChatCompletionRequest BuildChatRequest(AiExecutionRequest request, string modelId)
    {
        var chatRequest = new ChatCompletionRequest(
            ModelId: modelId,
            Messages: request.Messages ?? new List<ChatMessage>(),
            Temperature: request.Temperature ?? 0.7f,
            MaxTokens: request.MaxTokens ?? 4096,
            SystemPrompt: request.SystemPrompt);

        if (!string.IsNullOrWhiteSpace(request.UserPrompt) && (request.Messages == null || request.Messages.Count == 0))
        {
            chatRequest = chatRequest with
            {
                Messages = new List<ChatMessage> { new("user", request.UserPrompt) }
            };
        }

        return chatRequest;
    }

    // ── Internal resolution logic ─────────────────────────────────────────

    private async Task<ResolvedExecution> ResolveExecutionAsync(
        AiExecutionRequest request,
        Guid userId,
        Guid tenantId,
        CancellationToken ct)
    {
        var appliedPolicies = new List<string>();

        // 1. Override explícito de admin
        if (request.TargetModelId.HasValue)
        {
            var overrideModel = await _modelCatalogService.ResolveModelByIdAsync(
                request.TargetModelId.Value, ct);
            if (overrideModel is not null)
            {
                var auth = await _authorizationService.ValidateModelAccessAsync(overrideModel.ModelId, ct);
                if (auth.IsAllowed)
                {
                    var provider = _providerFactory.GetChatProvider(overrideModel.ProviderId);
                    if (provider is not null && _healthMonitor.IsHealthy(overrideModel.ProviderId))
                    {
                        return ResolvedExecution.Available(
                            AiProviderType.Internal,
                            overrideModel.ProviderId,
                            overrideModel.ModelName,
                            overrideModel.DisplayName,
                            provider,
                            appliedPolicies);
                    }
                }
            }
        }

        // 2. User preference para (FeatureKey, UserId)
        var userPref = await _userPreferenceRepository.GetByUserAndFeatureAsync(
            userId, tenantId, request.FeatureKey, ct);

        if (userPref is not null && userPref.IsActive)
        {
            _logger.LogDebug(
                "User preference found for '{FeatureKey}': {PreferenceType}",
                request.FeatureKey, userPref.PreferenceType);

            switch (userPref.PreferenceType)
            {
                case AiPreferenceType.Disabled:
                    return ResolvedExecution.Unavailable(
                        AiAvailabilityStatus.DisabledByUser,
                        "IA desabilitada pelo usuário para esta funcionalidade.");

                case AiPreferenceType.Internal when userPref.PreferredModelId.HasValue:
                    var prefModel = await _modelCatalogService.ResolveModelByIdAsync(
                        userPref.PreferredModelId.Value, ct);
                    if (prefModel is not null)
                    {
                        var provider = _providerFactory.GetChatProvider(prefModel.ProviderId);
                        if (provider is not null && _healthMonitor.IsHealthy(prefModel.ProviderId))
                        {
                            return ResolvedExecution.Available(
                                AiProviderType.Internal,
                                prefModel.ProviderId,
                                prefModel.ModelName,
                                prefModel.DisplayName,
                                provider,
                                appliedPolicies);
                        }
                    }
                    break;

                case AiPreferenceType.ExternalProduct when userPref.ExternalProduct.HasValue:
                {
                    var (extProviderId, extModelId, extDisplayName) = MapExternalProductToProvider(
                        userPref.ExternalProduct!.Value, userPref.ExternalProductModel);
                    if (_healthMonitor.IsHealthy(extProviderId))
                    {
                        var extProvider = _providerFactory.GetChatProvider(extProviderId);
                        if (extProvider is not null)
                        {
                            return ResolvedExecution.Available(
                                AiProviderType.ExternalProduct,
                                extProviderId,
                                extModelId,
                                extDisplayName,
                                extProvider,
                                appliedPolicies);
                        }
                    }
                    break;
                }
            }

            // User preference falhou → tenta fallback se permitido
            if (!request.AllowFallback)
            {
                return ResolvedExecution.Unavailable(
                    AiAvailabilityStatus.NoProviderAvailable,
                    "Preferência do usuário não pôde ser atendida e fallback está desabilitado.");
            }
        }

        // 3. User preference global (FeatureKey = "*")
        var globalPref = await _userPreferenceRepository.GetByUserAndFeatureAsync(
            userId, tenantId, "*", ct);
        if (globalPref is not null && globalPref.IsActive)
        {
            if (globalPref.PreferenceType == AiPreferenceType.Disabled)
            {
                return ResolvedExecution.Unavailable(
                    AiAvailabilityStatus.DisabledByUser,
                    "IA desabilitada globalmente pelo usuário.");
            }
        }

        // 4. Tenant Feature-Model Binding
        var binding = await _featureBindingRepository.GetByFeatureKeyAsync(
            request.FeatureKey, tenantId, ct);

        if (binding is not null && binding.IsActive)
        {
            var bindingMode = binding.Mode;

            if (bindingMode == AiBindingMode.Disabled)
            {
                return ResolvedExecution.Unavailable(
                    AiAvailabilityStatus.DisabledByTenant,
                    "IA desabilitada pelo administrador do tenant para esta funcionalidade.");
            }

            if (bindingMode == AiBindingMode.Internal || bindingMode == default)
            {
                var boundModel = await _modelCatalogService.ResolveModelForFeatureAsync(
                    request.FeatureKey, request.RequestType, tenantId, ct);
                if (boundModel is not null && _healthMonitor.IsHealthy(boundModel.ProviderId))
                {
                    var provider = _providerFactory.GetChatProvider(boundModel.ProviderId);
                    if (provider is not null)
                    {
                        return ResolvedExecution.Available(
                            AiProviderType.Internal,
                            boundModel.ProviderId,
                            boundModel.ModelName,
                            boundModel.DisplayName,
                            provider,
                            appliedPolicies,
                            WasFallbackUsed: true);
                    }
                }
            }
        }

        // 5. System default
        var defaultModel = await _modelCatalogService.ResolveDefaultModelAsync(
            request.RequestType, ct);
        if (defaultModel is not null && _healthMonitor.IsHealthy(defaultModel.ProviderId))
        {
            var provider = _providerFactory.GetChatProvider(defaultModel.ProviderId);
            if (provider is not null)
            {
                return ResolvedExecution.Available(
                    AiProviderType.Internal,
                    defaultModel.ProviderId,
                    defaultModel.ModelName,
                    defaultModel.DisplayName,
                    provider,
                    appliedPolicies,
                    WasFallbackUsed: true);
            }
        }

        // 5b. System default with latency-aware fallback among multiple options
        // If the default provider is unhealthy, try to find another healthy default
        var fallbackModel = await ResolveLatencyAwareFallbackAsync(request.RequestType, ct);
        if (fallbackModel is not null)
        {
            var provider = _providerFactory.GetChatProvider(fallbackModel.ProviderId);
            if (provider is not null)
            {
                return ResolvedExecution.Available(
                    AiProviderType.Internal,
                    fallbackModel.ProviderId,
                    fallbackModel.ModelName,
                    fallbackModel.DisplayName,
                    provider,
                    appliedPolicies,
                    WasFallbackUsed: true);
            }
        }

        // 6. HARD FALLBACK: NullProvider
        var nullProvider = _providerFactory.GetChatProvider("null");
        if (nullProvider is not null)
        {
            return ResolvedExecution.Available(
                AiProviderType.Null,
                "null",
                "null",
                "IA Desabilitada",
                nullProvider,
                appliedPolicies,
                WasFallbackUsed: true);
        }

        return ResolvedExecution.Unavailable(
            AiAvailabilityStatus.NoProviderAvailable,
            "Nenhum provider de IA disponível.");
    }

    /// <summary>
    /// Tenta resolver um modelo fallback saudável ordenado por latência.
    /// Usado quando o provider/modelo padrão está indisponível.
    /// </summary>
    private async Task<ResolvedModel?> ResolveLatencyAwareFallbackAsync(
        string requestType,
        CancellationToken ct)
    {
        var healthyProviders = _healthMonitor.GetHealthyProviderIds();
        if (healthyProviders.Count == 0)
            return null;

        // Rank by latency and pick the fastest
        var rankings = await _routingMetrics.RankProvidersByLatencyAsync(healthyProviders, ct);
        foreach (var ranking in rankings)
        {
            var model = await InferModelForProviderAsync(ranking.ProviderId, ct);
            if (!string.IsNullOrWhiteSpace(model))
            {
                return new ResolvedModel(
                    Guid.Empty,
                    model,
                    $"{model} (fallback)",
                    ranking.ProviderId,
                    ranking.ProviderId,
                    false,
                    "chat",
                    null);
            }
        }

        // No metrics available — pick first healthy provider
        foreach (var providerId in healthyProviders)
        {
            var model = await InferModelForProviderAsync(providerId, ct);
            if (!string.IsNullOrWhiteSpace(model))
            {
                return new ResolvedModel(
                    Guid.Empty,
                    model,
                    $"{model} (fallback)",
                    providerId,
                    providerId,
                    false,
                    "chat",
                    null);
            }
        }

        return null;
    }

    private static (string ProviderId, string ModelId, string DisplayName) MapExternalProductToProvider(
        ExternalAiProductType product, string? productModel)
    {
        return product switch
        {
            ExternalAiProductType.ChatGPT => ("openai", productModel ?? "gpt-4o-mini", "ChatGPT"),
            ExternalAiProductType.ClaudeCode => ("anthropic", productModel ?? "claude-sonnet-4-6", "Claude"),
            ExternalAiProductType.Gemini => ("gemini", productModel ?? "gemini-1.5-pro", "Gemini"),
            ExternalAiProductType.GitHubCopilot => ("github-copilot", productModel ?? "copilot-chat", "GitHub Copilot"),
            _ => ("openai", productModel ?? "gpt-4o-mini", "IA Externa")
        };
    }

    private static int EstimateTokens(AiExecutionRequest request)
    {
        var text = request.UserPrompt ?? string.Empty;
        if (request.Messages is not null)
        {
            text = string.Join(" ", request.Messages.Select(m => m.Content));
        }
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            text += " " + request.SystemPrompt;
        }
        // Heurística simples: ~4 chars por token
        return Math.Max(1, text.Length / 4) + 512; // +512 para resposta estimada
    }

    private static AiExecutionResult BuildErrorResult(
        AiAvailabilityStatus status,
        string message,
        TimeSpan duration)
    {
        return new AiExecutionResult(
            Success: false,
            Content: null,
            ProviderType: AiProviderType.Null,
            ResolvedProviderId: "null",
            ResolvedModelId: "null",
            ResolvedModelDisplayName: "IA Desabilitada",
            PromptTokens: 0,
            CompletionTokens: 0,
            Duration: duration,
            RoutingDecisionId: null,
            WasFallbackUsed: false,
            ErrorMessage: $"[{status}] {message}");
    }

    // Helper record for internal resolution
    private sealed record ResolvedExecution(
        bool IsAvailable,
        AiAvailabilityStatus Status,
        AiProviderType ProviderType,
        string ProviderId,
        string ModelId,
        string ModelDisplayName,
        IChatCompletionProvider? Provider,
        List<string> AppliedPolicies,
        string? UnavailabilityReason = null,
        bool WasFallbackUsed = false)
    {
        public static ResolvedExecution Available(
            AiProviderType providerType,
            string providerId,
            string modelId,
            string displayName,
            IChatCompletionProvider provider,
            List<string> policies,
            bool WasFallbackUsed = false)
            => new(true, AiAvailabilityStatus.Available, providerType, providerId, modelId, displayName, provider, policies, null, WasFallbackUsed);

        public static ResolvedExecution Unavailable(
            AiAvailabilityStatus status,
            string reason)
            => new(false, status, AiProviderType.Null, "null", "null", "IA Desabilitada", null, new List<string>(), reason);
    }
}
