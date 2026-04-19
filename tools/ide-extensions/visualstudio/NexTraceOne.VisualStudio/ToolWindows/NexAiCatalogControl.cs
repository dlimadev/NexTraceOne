using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Controlo WPF do catálogo de serviços NexTraceOne.
/// Apresenta uma TreeView com os serviços registados no catálogo, permitindo expandir
/// cada serviço para ver equipa, domínio, tipo e linguagem.
/// Inclui acções de contexto: abrir no dashboard e consultar o AI Chat.
/// </summary>
public sealed partial class NexAiCatalogControl : UserControl
{
    private readonly ToolWindowPane _parentWindow;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;

    private TreeView? _treeView;
    private Button? _refreshBtn;
    private TextBlock? _statusText;

    public NexAiCatalogControl(ToolWindowPane parentWindow)
    {
        _parentWindow = parentWindow;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        InitializeComponent();
        _ = LoadServicesAsync();
    }

    private NexTraceOneOptionsPage? GetOptions()
    {
        return _parentWindow.GetDialogPage(typeof(NexTraceOneOptionsPage)) as NexTraceOneOptionsPage
               ?? (_parentWindow.Package as AsyncPackage)?.GetDialogPage(typeof(NexTraceOneOptionsPage)) as NexTraceOneOptionsPage;
    }

    private void InitializeComponent()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ── Header ───────────────────────────────────────────────────────────
        var header = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            Margin = new Thickness(0)
        };

        _refreshBtn = new Button
        {
            Content = "⟳ Refresh",
            Width = 70,
            Height = 22,
            FontSize = 10,
            Margin = new Thickness(4),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.LightGray,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        _refreshBtn.Click += OnRefreshClick;
        DockPanel.SetDock(_refreshBtn, Dock.Right);
        header.Children.Add(_refreshBtn);

        header.Children.Add(new TextBlock
        {
            Text = "Service Catalog",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 4, 4, 4)
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ── Tree View ─────────────────────────────────────────────────────────
        _treeView = new TreeView
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderThickness = new Thickness(0),
            Foreground = Brushes.LightGray
        };
        Grid.SetRow(_treeView, 1);
        root.Children.Add(_treeView);

        // ── Status bar ────────────────────────────────────────────────────────
        _statusText = new TextBlock
        {
            Text = "Loading…",
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
            FontSize = 10,
            Margin = new Thickness(6, 2, 6, 2)
        };
        Grid.SetRow(_statusText, 2);
        root.Children.Add(_statusText);

        Content = root;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _ = LoadServicesAsync();
    }

    private async Task LoadServicesAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        SetStatus("Loading…");
        _treeView!.Items.Clear();
        if (_refreshBtn is not null) _refreshBtn.IsEnabled = false;

        var options = GetOptions();
        if (options is null || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            SetStatus("API key not configured — go to Tools → Options → NexTraceOne");
            if (_refreshBtn is not null) _refreshBtn.IsEnabled = true;
            return;
        }

        List<CatalogServiceDto>? services = null;
        string? error = null;

        try
        {
            services = await FetchServicesAsync(options.ServerUrl, options.ApiKey, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (_refreshBtn is not null) _refreshBtn.IsEnabled = true;

        if (error is not null)
        {
            SetStatus($"Error: {error}");
            return;
        }

        if (services is null || services.Count == 0)
        {
            SetStatus("No services found.");
            return;
        }

        foreach (var svc in services)
        {
            var serviceItem = BuildServiceTreeItem(svc, options);
            _treeView!.Items.Add(serviceItem);
        }

        SetStatus($"{services.Count} service(s) loaded.");
    }

    private TreeViewItem BuildServiceTreeItem(CatalogServiceDto svc, NexTraceOneOptionsPage options)
    {
        var item = new TreeViewItem
        {
            Header = BuildServiceHeader(svc),
            Foreground = Brushes.LightGray,
            Background = Brushes.Transparent,
            IsExpanded = false,
            Tag = svc
        };

        if (!string.IsNullOrWhiteSpace(svc.Description))
            item.Items.Add(MakeInfoItem($"ℹ {svc.Description}"));
        if (!string.IsNullOrWhiteSpace(svc.TeamName))
            item.Items.Add(MakeInfoItem($"👥 Team: {svc.TeamName}"));
        if (!string.IsNullOrWhiteSpace(svc.Domain))
            item.Items.Add(MakeInfoItem($"◎ Domain: {svc.Domain}"));
        if (!string.IsNullOrWhiteSpace(svc.Type))
            item.Items.Add(MakeInfoItem($"⬡ Type: {svc.Type}"));
        if (!string.IsNullOrWhiteSpace(svc.Language))
            item.Items.Add(MakeInfoItem($"⌨ Language: {svc.Language}"));
        if (!string.IsNullOrWhiteSpace(svc.Status))
            item.Items.Add(MakeInfoItem($"● Status: {svc.Status}"));

        // Action: Open in dashboard
        var dashItem = new TreeViewItem
        {
            Header = new TextBlock
            {
                Text = "↗ Open in Dashboard",
                Foreground = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
                FontSize = 11
            },
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        dashItem.MouseDoubleClick += (_, _) =>
        {
            var url = $"{options.ServerUrl.TrimEnd('/')}/services/{Uri.EscapeDataString(svc.Name)}";
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* non-fatal */ }
        };
        item.Items.Add(dashItem);

        // Action: Ask AI
        var aiItem = new TreeViewItem
        {
            Header = new TextBlock
            {
                Text = "💬 Ask AI about this service",
                Foreground = new SolidColorBrush(Color.FromRgb(181, 206, 168)),
                FontSize = 11
            },
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        aiItem.MouseDoubleClick += (_, _) => OpenChatWithQuery(
            $"Show service context, ownership, contracts and recent changes for: {svc.Name}");
        item.Items.Add(aiItem);

        return item;
    }

    private static UIElement BuildServiceHeader(CatalogServiceDto svc)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock
        {
            Text = "⬡ ",
            Foreground = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = svc.Name,
            Foreground = Brushes.LightGray,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (!string.IsNullOrWhiteSpace(svc.TeamName))
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"  {svc.TeamName}",
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        return panel;
    }

    private static TreeViewItem MakeInfoItem(string text) => new()
    {
        Header = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
            FontSize = 11
        },
        Background = Brushes.Transparent,
        IsEnabled = false
    };

    private void OpenChatWithQuery(string query)
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_parentWindow.Package is AsyncPackage pkg)
            {
                var chatWindow = await pkg.ShowToolWindowAsync(
                    typeof(NexAiChatWindow), 0, create: true, cancellationToken: pkg.DisposalToken)
                    as NexAiChatWindow;
                chatWindow?.SendQuery(query);
            }
        });
    }

    private void SetStatus(string text)
    {
        if (_statusText is not null) _statusText.Text = text;
    }

    // ── HTTP ─────────────────────────────────────────────────────────────────

    private async Task<List<CatalogServiceDto>> FetchServicesAsync(
        string serverUrl, string apiKey, CancellationToken cancellationToken)
    {
        var url = $"{serverUrl.TrimEnd('/')}/api/v1/catalog/services?pageSize=100";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var items = root.ValueKind == JsonValueKind.Array
            ? root
            : (root.TryGetProperty("items", out var arr) ? arr : default);

        var result = new List<CatalogServiceDto>();
        if (items.ValueKind != JsonValueKind.Array) return result;

        foreach (var el in items.EnumerateArray())
        {
            result.Add(new CatalogServiceDto
            {
                Name = GetString(el, "name") ?? "(unknown)",
                TeamName = GetString(el, "teamName"),
                Domain = GetString(el, "domain"),
                Type = GetString(el, "type"),
                Language = GetString(el, "language"),
                Status = GetString(el, "status"),
                Description = GetString(el, "description")
            });
        }
        return result;
    }

    private static string? GetString(JsonElement el, string property)
    {
        return el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;
    }
}

/// <summary>DTO simples para um serviço do catálogo NexTraceOne.</summary>
internal sealed class CatalogServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? Domain { get; set; }
    public string? Type { get; set; }
    public string? Language { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}
