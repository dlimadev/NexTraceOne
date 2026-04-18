using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using NexTraceOne.VisualStudio.ToolWindows;

namespace NexTraceOne.VisualStudio.Commands;

/// <summary>
/// Comando para abrir o Tool Window do catálogo de serviços NexTraceOne.
/// Acessível via View → Other Windows → NexTraceOne: Service Catalog.
/// </summary>
internal sealed class NexCatalogWindowCommand
{
    private const int CommandId = 0x0103;
    private readonly AsyncPackage _package;

    private NexCatalogWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));

        var menuCommandId = new CommandID(PackageGuids.CommandSetGuid, CommandId);
        var menuItem = new MenuCommand(Execute, menuCommandId);
        commandService.AddCommand(menuItem);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService is not null)
        {
            _ = new NexCatalogWindowCommand(package, commandService);
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _package.JoinableTaskFactory.RunAsync(async delegate
        {
            var window = await _package.ShowToolWindowAsync(
                typeof(NexAiCatalogWindow),
                0,
                create: true,
                cancellationToken: _package.DisposalToken);

            if (window?.Frame is not IVsWindowFrame windowFrame)
                throw new NotSupportedException("Cannot create NexTraceOne Service Catalog window.");

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        });
    }
}
