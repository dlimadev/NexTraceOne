using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexTraceOne.VisualStudio.Commands;

/// <summary>
/// Comando "Generate Contract Migration Patch" disponível no menu Tools do Visual Studio.
/// Abre um diálogo para o utilizador introduzir as versões base e alvo do contrato,
/// escolher o target (provider/consumer/all) e a linguagem de implementação,
/// e exibe as sugestões de código de migração no Output Window do Visual Studio.
/// </summary>
internal sealed class NexContractMigrationCommand
{
    private const int CommandId = 0x0104;
    private readonly AsyncPackage _package;
    private readonly HttpClient _httpClient;

    private NexContractMigrationCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var menuCommandId = new CommandID(PackageGuids.CommandSetGuid, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandId);
        commandService.AddCommand(menuItem);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService is not null)
        {
            _ = new NexContractMigrationCommand(package, commandService);
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        _package.JoinableTaskFactory.RunAsync(async delegate
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var options = GetOptions();
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "NexTraceOne API key not configured. Go to Tools → Options → NexTraceOne → General.",
                    "NexTraceOne: Contract Migration",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            await RunMigrationWizardAsync(options);
        });
    }

    private async Task RunMigrationWizardAsync(NexMigrationOptions options)
    {
        // Step 1: Collect base version ID
        var baseVersionId = await PromptInputAsync(
            "NexTraceOne: Contract Migration Patch",
            "Enter the BASE contract version ID (GUID):",
            null);
        if (string.IsNullOrWhiteSpace(baseVersionId) || !Guid.TryParse(baseVersionId, out _))
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(_package,
                "Invalid or empty base version ID. Must be a valid GUID.",
                "NexTraceOne: Contract Migration",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        // Step 2: Collect target version ID
        var targetVersionId = await PromptInputAsync(
            "NexTraceOne: Contract Migration Patch",
            "Enter the TARGET contract version ID (GUID):",
            null);
        if (string.IsNullOrWhiteSpace(targetVersionId) || !Guid.TryParse(targetVersionId, out _))
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(_package,
                "Invalid or empty target version ID. Must be a valid GUID.",
                "NexTraceOne: Contract Migration",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        // Step 3: Call migration patch API
        MigrationPatchResult? patch = null;
        try
        {
            patch = await CallMigrationPatchApiAsync(
                options, baseVersionId, targetVersionId,
                "all", "C#", _package.DisposalToken);
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(
                _package,
                $"Migration patch API error: {ex.Message}",
                "NexTraceOne: Contract Migration",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        // Step 4: Display results in Output Window
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        DisplayMigrationPatch(patch);
    }

    private async Task<MigrationPatchResult?> CallMigrationPatchApiAsync(
        NexMigrationOptions options,
        string baseVersionId,
        string targetVersionId,
        string target,
        string language,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            baseVersionId,
            targetVersionId,
            target,
            language
        });

        var url = $"{options.ServerUrl.TrimEnd('/')}/api/v1/contracts/migration-patch";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Server returned {(int)response.StatusCode}: {body}");

        return JsonSerializer.Deserialize<MigrationPatchResult>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private void DisplayMigrationPatch(MigrationPatchResult? patch)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
        if (outputWindow is null) return;

        var paneGuid = new Guid("A7B5E3C2-1D4F-4B8A-9E2F-3C1D7B5A8F0E");
        outputWindow.CreatePane(ref paneGuid, "NexTraceOne Migration", 1, 1);
        outputWindow.GetPane(ref paneGuid, out var pane);
        pane?.Activate();

        if (patch is null)
        {
            pane?.OutputStringThreadSafe("NexTraceOne: No migration patch data received.\n");
            return;
        }

        pane?.OutputStringThreadSafe(
            $"\n=== NexTraceOne: Contract Migration Patch ===\n" +
            $"Protocol: {patch.Protocol}  Language: {patch.Language}  " +
            $"Change Level: {patch.ChangeLevel}  Breaking Changes: {patch.BreakingChangeCount}\n" +
            $"Generated At: {patch.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC\n\n");

        if (patch.ProviderSuggestions is { Count: > 0 })
        {
            pane?.OutputStringThreadSafe("--- PROVIDER SUGGESTIONS ---\n");
            foreach (var s in patch.ProviderSuggestions)
            {
                pane?.OutputStringThreadSafe($"[{s.Severity.ToUpperInvariant()}] [{s.Kind}] {s.Description}\n");
                if (!string.IsNullOrWhiteSpace(s.CodeHint))
                    pane?.OutputStringThreadSafe($"{s.CodeHint}\n\n");
            }
        }

        if (patch.ConsumerSuggestions is { Count: > 0 })
        {
            pane?.OutputStringThreadSafe("\n--- CONSUMER SUGGESTIONS ---\n");
            foreach (var s in patch.ConsumerSuggestions)
            {
                pane?.OutputStringThreadSafe($"[{s.Severity.ToUpperInvariant()}] [{s.Kind}] {s.Description}\n");
                if (!string.IsNullOrWhiteSpace(s.CodeHint))
                    pane?.OutputStringThreadSafe($"{s.CodeHint}\n\n");
            }
        }

        if ((patch.ProviderSuggestions?.Count ?? 0) == 0 && (patch.ConsumerSuggestions?.Count ?? 0) == 0)
        {
            pane?.OutputStringThreadSafe("No migration suggestions generated (no detectable changes between versions).\n");
        }

        pane?.OutputStringThreadSafe("=== End of Migration Patch ===\n\n");
    }

    private static Task<string?> PromptInputAsync(string title, string prompt, string? defaultValue)
    {
        // Simple synchronous dialog — runs on UI thread via the caller
        var result = Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultValue ?? string.Empty);
        return Task.FromResult(string.IsNullOrWhiteSpace(result) ? null : result);
    }

    private static NexMigrationOptions GetOptions()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var page = (NexTraceOneOptionsPage?)Package.GetGlobalService(typeof(NexTraceOneOptionsPage));
        return page is not null
            ? new NexMigrationOptions(page.ServerUrl, page.ApiKey)
            : new NexMigrationOptions("http://localhost:5000", string.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API models
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class MigrationPatchResult
    {
        public string Protocol { get; init; } = string.Empty;
        public string Language { get; init; } = string.Empty;
        public string ChangeLevel { get; init; } = string.Empty;
        public int BreakingChangeCount { get; init; }
        public List<MigrationSuggestion> ProviderSuggestions { get; init; } = [];
        public List<MigrationSuggestion> ConsumerSuggestions { get; init; } = [];
        public DateTime GeneratedAt { get; init; }
    }

    private sealed class MigrationSuggestion
    {
        public string Kind { get; init; } = string.Empty;
        public string Side { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? CodeHint { get; init; }
        public string Severity { get; init; } = string.Empty;
    }

    private sealed record NexMigrationOptions(string ServerUrl, string ApiKey);
}
