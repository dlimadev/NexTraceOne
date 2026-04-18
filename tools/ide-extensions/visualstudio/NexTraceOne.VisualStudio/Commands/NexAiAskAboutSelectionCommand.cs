using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using NexTraceOne.VisualStudio.ToolWindows;

namespace NexTraceOne.VisualStudio.Commands;

/// <summary>
/// Comando "Ask NexTraceOne AI" acessível via clique direito no editor.
/// Envia o texto selecionado para o Chat AI como contexto da query.
/// </summary>
internal sealed class NexAiAskAboutSelectionCommand
{
    private const int CommandId = 0x0101;
    private readonly AsyncPackage _package;

    private NexAiAskAboutSelectionCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));

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
            _ = new NexAiAskAboutSelectionCommand(package, commandService);
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _package.JoinableTaskFactory.RunAsync(async delegate
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var selectedText = GetSelectedText();
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "Please select some code or text first.",
                    "NexTraceOne AI",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var window = await _package.ShowToolWindowAsync(
                typeof(NexAiChatWindow),
                0,
                create: true,
                cancellationToken: _package.DisposalToken);

            if (window is NexAiChatWindow chatWindow)
            {
                var query = $"Analyse and explain this code in the context of NexTraceOne services and contracts:\n\n{selectedText}";
                chatWindow.SendQuery(query);
            }

            if (window?.Frame is IVsWindowFrame windowFrame)
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        });
    }

    private static string? GetSelectedText()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
        if (textManager is null) return null;

        textManager.GetActiveView(1, null, out var textView);
        if (textView is null) return null;

        textView.GetSelectedText(out var selectedText);
        return selectedText;
    }
}
