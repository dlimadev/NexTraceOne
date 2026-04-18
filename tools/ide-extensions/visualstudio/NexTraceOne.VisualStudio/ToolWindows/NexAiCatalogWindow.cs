using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Tool Window do catálogo de serviços do NexTraceOne para Visual Studio 2022.
/// Apresenta uma TreeView com os serviços registados, permitindo inspeccionar detalhes
/// e solicitar contexto AI diretamente do IDE.
/// </summary>
[Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
public sealed class NexAiCatalogWindow : ToolWindowPane
{
    private NexAiCatalogControl? _control;

    public NexAiCatalogWindow() : base(null)
    {
        Caption = "NexTraceOne: Service Catalog";
        BitmapResourceID = 301;
        BitmapIndex = 1;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _control = new NexAiCatalogControl(this);
        Content = _control;
    }
}
