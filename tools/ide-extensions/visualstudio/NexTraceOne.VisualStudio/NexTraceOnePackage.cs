using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace NexTraceOne.VisualStudio;

/// <summary>
/// AsyncPackage principal da extensão NexTraceOne para Visual Studio 2022.
/// Regista o Tool Window de Chat AI, menus contextuais e a página de opções.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuids.PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(NexAiChatWindow), Style = VsDockStyle.Tabbed, DockedWidth = 380)]
[ProvideOptionPage(typeof(NexTraceOneOptionsPage), "NexTraceOne", "General", 0, 0, true)]
public sealed class NexTraceOnePackage : AsyncPackage
{
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // Switch to UI thread to register commands
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await NexAiChatWindowCommand.InitializeAsync(this);
        await NexAiAskAboutSelectionCommand.InitializeAsync(this);
    }
}

/// <summary>Constantes de identificadores de GUIDs da extensão.</summary>
internal static class PackageGuids
{
    public const string PackageGuidString = "8c5d71e4-2f3a-4b6e-9a1c-7e3f0d8b2c4a";
    public static readonly Guid CommandSetGuid = new("1a2b3c4d-5e6f-7a8b-9c0d-e1f2a3b4c5d6");
}
