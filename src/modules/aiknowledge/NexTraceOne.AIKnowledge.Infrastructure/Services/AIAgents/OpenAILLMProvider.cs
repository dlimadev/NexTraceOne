namespace NexTraceOne.AIKnowledge.Infrastructure.Services.AIAgents;

/// <summary>
/// Implementação stub do ILLMProvider para não bloquear compilação.
/// TODO: Implementar com SDK real da OpenAI quando disponível.
/// </summary>
public class OpenAILLMProvider : ILLMProvider
{
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAILLMProvider(string apiKey, string model = "gpt-4")
    {
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        // TODO: Implementar chamada real à API da OpenAI
        await Task.Delay(10);
        return $"[STUB] Response from {_model} for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }

    public async Task<List<string>> BatchGenerateAsync(List<string> prompts)
    {
        var results = new List<string>();
        foreach (var prompt in prompts)
        {
            results.Add(await GenerateAsync(prompt));
        }
        return results;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_apiKey);
    }
}