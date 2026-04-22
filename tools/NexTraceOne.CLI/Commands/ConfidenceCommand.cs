using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex confidence' — consulta o change confidence score de uma release no NexTraceOne.
/// Subcomandos: score.
/// Útil em pipelines CI/CD para bloquear promoção de releases abaixo do threshold configurado.
/// </summary>
public static class ConfidenceCommand
{
    private const int ExitSuccess = 0;
    private const int ExitFailed = 1;
    private const int ExitError = 2;

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Command Create()
    {
        var command = new Command("confidence", "Consult and gate on change confidence scores.");
        command.Add(CreateScoreCommand());
        return command;
    }

    private static Command CreateScoreCommand()
    {
        var releaseIdArg = new Argument<string>("releaseId")
        {
            Description = "The release identifier to score."
        };
        var urlOpt = new Option<string>("--url")
        {
            Description = "NexTraceOne API base URL.",
            DefaultValueFactory = _ => CliConfig.ResolveUrl(null)
        };
        var tokenOpt = new Option<string>("--token")
        {
            Description = "API authentication token."
        };
        var minScoreOpt = new Option<int>("--min-score")
        {
            Description = "Minimum acceptable confidence score. Exit 1 if below threshold.",
            DefaultValueFactory = _ => 0
        };
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format: text (default) or json.",
            DefaultValueFactory = _ => "text"
        };

        var scoreCmd = new Command("score", "Get the confidence score for a release.");
        scoreCmd.Add(releaseIdArg);
        scoreCmd.Add(urlOpt);
        scoreCmd.Add(tokenOpt);
        scoreCmd.Add(minScoreOpt);
        scoreCmd.Add(formatOpt);

        scoreCmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var releaseId = parseResult.GetValue(releaseIdArg)!;
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var minScore = parseResult.GetValue(minScoreOpt);
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await FetchAndDisplayScoreAsync(releaseId, url, token, minScore, format, cancellationToken)
                .ConfigureAwait(false);
        });

        return scoreCmd;
    }

    private static async Task<int> FetchAndDisplayScoreAsync(
        string releaseId, string serverUrl, string? token, int minScore, string format, CancellationToken ct)
    {
        using var client = CreateHttpClient(serverUrl, token);

        try
        {
            var response = await client
                .GetAsync($"/api/v1/changes/{Uri.EscapeDataString(releaseId)}/confidence", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]✗ API error {(int)response.StatusCode}[/]");
                return ExitError;
            }

            var confidence = await response.Content
                .ReadFromJsonAsync<ConfidenceResponse>(JsonReadOptions, ct)
                .ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(confidence, JsonPrintOptions));
            }
            else
            {
                var score = confidence?.Score ?? 0;
                var tier = confidence?.Tier ?? "Unknown";
                var (icon, color) = score >= 70 ? ("✓", "green") : ("✗", "red");

                AnsiConsole.MarkupLine($"[{color}]{icon} Confidence Score: {score:F1}[/]  Tier: {tier.EscapeMarkup()}");

                if (confidence?.Recommendation is not null)
                    AnsiConsole.MarkupLine($"  [dim]Recommendation:[/] {confidence.Recommendation.EscapeMarkup()}");
            }

            if (minScore > 0 && (confidence?.Score ?? 0) < minScore)
            {
                AnsiConsole.MarkupLine($"[red]✗ Score {confidence?.Score:F1} is below minimum threshold {minScore}.[/]");
                return ExitFailed;
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Unreachable[/]  {serverUrl.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15)
        };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class ConfidenceResponse
    {
        [JsonPropertyName("releaseId")]
        public string? ReleaseId { get; init; }

        [JsonPropertyName("score")]
        public double Score { get; init; }

        [JsonPropertyName("tier")]
        public string? Tier { get; init; }

        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; init; }
    }
}
