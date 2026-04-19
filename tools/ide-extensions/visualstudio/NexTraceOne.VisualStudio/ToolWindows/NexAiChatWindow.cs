using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Tool Window do chat AI do NexTraceOne para Visual Studio 2022.
/// Apresenta uma interface de chat similar à do VS Code, usando WebView2.
/// Comunica com /api/v1/ai/ide/query do servidor NexTraceOne.
/// </summary>
[Guid("f3c2e1d0-b9a8-4f7e-8d6c-5b4a3c2d1e0f")]
public sealed class NexAiChatWindow : ToolWindowPane
{
    private NexAiChatControl? _control;

    public NexAiChatWindow() : base(null)
    {
        Caption = "NexTraceOne AI Chat";
        BitmapResourceID = 301;
        BitmapIndex = 1;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _control = new NexAiChatControl(this);
        Content = _control;
    }

    /// <summary>Abre o tool window e envia uma query pré-preenchida.</summary>
    public void SendQuery(string query)
    {
        _control?.EnqueueQuery(query);
    }
}
