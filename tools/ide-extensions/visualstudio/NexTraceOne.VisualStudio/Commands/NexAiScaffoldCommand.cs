using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NexTraceOne.VisualStudio.ToolWindows;

namespace NexTraceOne.VisualStudio.Commands;

/// <summary>
/// Comando "Scaffold New Service" disponível no menu Tools do Visual Studio.
/// Guia o utilizador por um wizard para escolher template, nome do serviço e directório
/// de saída, depois gera os ficheiros do projecto e opcionalmente importa o contrato base.
/// </summary>
internal sealed class NexAiScaffoldCommand
{
    private const int CommandId = 0x0102;
    private readonly AsyncPackage _package;
    private readonly HttpClient _httpClient;

    private NexAiScaffoldCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            _ = new NexAiScaffoldCommand(package, commandService);
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
                    "NexTraceOne Scaffold",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            await RunScaffoldWizardAsync(options);
        });
    }

    private async Task RunScaffoldWizardAsync(NexOptions options)
    {
        // Step 1: Fetch templates
        TemplateSummary[]? templates = null;
        try
        {
            templates = await FetchTemplatesAsync(options, _package.DisposalToken);
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(
                _package,
                $"Could not fetch service templates: {ex.Message}",
                "NexTraceOne Scaffold",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        if (templates is null || templates.Length == 0)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(
                _package,
                "No active service templates found in NexTraceOne. Configure templates in the platform first.",
                "NexTraceOne Scaffold",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        // Step 2: Show scaffold dialog
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var solutionDir = GetSolutionDirectory();
        var dialog = new NexScaffoldDialog(templates, solutionDir);

        if (dialog.ShowDialog() != true)
            return;

        var serviceName = dialog.ServiceName;
        var selectedTemplate = dialog.SelectedTemplate;
        var team = dialog.TeamName;
        var domain = dialog.Domain;
        var outputDir = dialog.OutputDirectory;

        // Step 3: Call scaffold API
        ScaffoldPlan? plan = null;
        try
        {
            plan = await CallScaffoldApiAsync(options, selectedTemplate.Slug, serviceName, team, domain, _package.DisposalToken);
        }
        catch (Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                _package,
                $"Scaffold API error: {ex.Message}",
                "NexTraceOne Scaffold",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        // Step 4: Write files
        var filesWritten = 0;
        Directory.CreateDirectory(outputDir);

        foreach (var file in plan.Files)
        {
            if (file.Path is null) continue;
            try
            {
                var fullPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(outputDir, file.Path.Replace('/', System.IO.Path.DirectorySeparatorChar)));

                // Security: prevent path traversal
                if (!fullPath.StartsWith(outputDir + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    continue;

                var fileDir = System.IO.Path.GetDirectoryName(fullPath);
                if (fileDir is not null) Directory.CreateDirectory(fileDir);
                await File.WriteAllTextAsync(fullPath, file.Content ?? string.Empty, Encoding.UTF8, _package.DisposalToken)
                    .ConfigureAwait(false);
                filesWritten++;
            }
            catch { /* skip individual file errors */ }
        }

        // Write base contract if available
        if (!string.IsNullOrWhiteSpace(plan.BaseContractSpec))
        {
            var ext = plan.ServiceType?.ToUpperInvariant() switch
            {
                "RESTAPI" or "REST" => "openapi.json",
                "KAFKA" or "EVENTCONSUMER" or "EVENTPRODUCER" => "asyncapi.yaml",
                "SOAP" => "service.wsdl",
                _ => "contract.json"
            };
            var contractPath = System.IO.Path.Combine(outputDir, $"{serviceName}-{ext}");
            try
            {
                await File.WriteAllTextAsync(contractPath, plan.BaseContractSpec, Encoding.UTF8, _package.DisposalToken)
                    .ConfigureAwait(false);
                filesWritten++;
            }
            catch { /* non-fatal */ }
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var result = MessageBox.Show(
            $"✓ Scaffolding complete!\n{filesWritten} file(s) created in:\n{outputDir}\n\nOpen the folder in Windows Explorer?",
            "NexTraceOne Scaffold",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
            System.Diagnostics.Process.Start("explorer.exe", outputDir);

        // Open the project if it contains a .csproj
        if (dialog.OpenAfterScaffolding)
        {
            var csprojFiles = Directory.GetFiles(outputDir, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length > 0)
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                dte?.ExecuteCommand("File.OpenProject", csprojFiles[0]);
            }
        }
    }

    private async Task<TemplateSummary[]> FetchTemplatesAsync(NexOptions options, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(options.ServerUrl.TrimEnd('/') + "/api/v1/catalog/templates?isActive=true"));
        request.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Server returned {(int)response.StatusCode}: {body}");

        var result = JsonSerializer.Deserialize<TemplateListResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Items ?? [];
    }

    private async Task<ScaffoldPlan> CallScaffoldApiAsync(
        NexOptions options, string templateSlug,
        string serviceName, string? team, string? domain,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { serviceName, teamName = team, domain });
        var url = $"{options.ServerUrl.TrimEnd('/')}/api/v1/catalog/templates/slug/{Uri.EscapeDataString(templateSlug)}/scaffold";

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Template '{templateSlug}' not found.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Server returned {(int)response.StatusCode}: {body}");

        return JsonSerializer.Deserialize<ScaffoldPlan>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Empty response from scaffold API.");
    }

    private static string? GetSolutionDirectory()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var solutionPath = dte?.Solution?.FullName;
            return solutionPath is { Length: > 0 }
                ? System.IO.Path.GetDirectoryName(solutionPath)
                : null;
        }
        catch { return null; }
    }

    private static NexOptions GetOptions()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var page = (NexTraceOneOptionsPage)Package.GetGlobalService(typeof(NexTraceOneOptionsPage));
        return page is not null
            ? new NexOptions(page.ServerUrl, page.ApiKey, page.Persona)
            : new NexOptions("http://localhost:5000", string.Empty, "Engineer");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API models
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed record TemplateSummary(
        string Slug,
        string DisplayName,
        string Description,
        string Version,
        string ServiceType,
        string Language,
        string DefaultDomain,
        string DefaultTeam,
        bool HasBaseContract,
        bool HasScaffoldingManifest,
        int UsageCount);

    private sealed record TemplateListResponse(TemplateSummary[] Items, int Total);

    private sealed record ScaffoldedFile(string? Path, string? Content);

    private sealed record ScaffoldPlan(
        Guid ScaffoldingId,
        string ServiceName,
        string? TemplateSlug,
        string? TemplateVersion,
        string? ServiceType,
        string? Language,
        string? Domain,
        string? TeamName,
        string? BaseContractSpec,
        ScaffoldedFile[] Files,
        string? RepositoryUrl);

    private sealed record NexOptions(string ServerUrl, string ApiKey, string Persona);
}
