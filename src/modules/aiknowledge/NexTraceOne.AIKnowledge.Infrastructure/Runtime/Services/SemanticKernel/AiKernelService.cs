using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.SemanticKernel;

/// <summary>
/// Implementação de IAiKernelService usando Microsoft Semantic Kernel.
/// Orquestra plugins SK e delega chat completion aos providers existentes (Ollama, OpenAI, etc.)
/// via um adapter IChatCompletionService customizado.
/// </summary>
public sealed class AiKernelService : IAiKernelService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<AiKernelService> _logger;

    public AiKernelService(
        IChatCompletionService chatCompletionService,
        ILogger<AiKernelService> logger)
    {
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

    public Kernel CreateKernel(string providerId, string modelId)
    {
        _logger.LogInformation("Creating Semantic Kernel for provider={Provider}, model={Model}", providerId, modelId);

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_chatCompletionService);

        var kernel = builder.Build();
        kernel.Data["ProviderId"] = providerId;
        kernel.Data["ModelId"] = modelId;
        return kernel;
    }

    public void RegisterPlugin(Kernel kernel, KernelPlugin plugin)
    {
        kernel.Plugins.Add(plugin);
        _logger.LogDebug("Registered plugin {PluginName}", plugin.Name);
    }

    public Task<FunctionResult> ExecutePluginAsync(
        Kernel kernel,
        string pluginName,
        string functionName,
        KernelArguments arguments,
        CancellationToken ct = default)
    {
        var function = kernel.Plugins.GetFunction(pluginName, functionName);
        return function.InvokeAsync(kernel, arguments, cancellationToken: ct);
    }

    public async Task<string> ExecuteChatAsync(
        Kernel kernel,
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default)
    {
        var chatHistory = new ChatHistory();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            chatHistory.AddSystemMessage(systemPrompt);
        }

        foreach (var message in messages)
        {
            chatHistory.AddMessage(
                message.Role.Equals("system", StringComparison.OrdinalIgnoreCase) ? AuthorRole.System :
                message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? AuthorRole.Assistant :
                AuthorRole.User,
                message.Content);
        }

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = 0.7,
                ["max_tokens"] = 4096
            }
        };

        try
        {
            var contents = await _chatCompletionService.GetChatMessageContentsAsync(chatHistory, settings, kernel, ct);
            return contents.Count > 0 ? contents[0].Content ?? string.Empty : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SK chat completion failed");
            return $"[Error: {ex.Message}]";
        }
    }
}
