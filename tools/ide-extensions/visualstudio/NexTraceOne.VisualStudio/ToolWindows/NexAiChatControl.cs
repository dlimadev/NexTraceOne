using Microsoft.VisualStudio.Shell;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Controlo WPF do chat AI do NexTraceOne.
/// Interface minimalista com histórico de mensagens, caixa de input e botão de envio.
/// </summary>
public sealed partial class NexAiChatControl : UserControl
{
    private readonly ToolWindowPane _parentWindow;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;

    private string? _pendingQuery;

    public NexAiChatControl(ToolWindowPane parentWindow)
    {
        _parentWindow = parentWindow;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        InitializeComponent();
    }

    /// <summary>Enfileira uma query para envio assim que o controlo estiver pronto.</summary>
    public void EnqueueQuery(string query)
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            InputBox.Text = query;
            await SendQueryAsync(query);
        });
    }

    private void InitializeComponent()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Messages area
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Name = "MessagesScroll"
        };
        MessagesPanel = new StackPanel { Margin = new Thickness(8) };
        scrollViewer.Content = MessagesPanel;
        Grid.SetRow(scrollViewer, 0);
        grid.Children.Add(scrollViewer);

        // Input area
        var inputPanel = new DockPanel { Margin = new Thickness(8, 4, 8, 8) };
        var sendBtn = new Button
        {
            Content = "Send",
            Width = 60,
            Margin = new Thickness(4, 0, 0, 0)
        };
        sendBtn.Click += OnSendClick;
        DockPanel.SetDock(sendBtn, Dock.Right);
        inputPanel.Children.Add(sendBtn);

        InputBox = new TextBox
        {
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        InputBox.KeyDown += OnInputKeyDown;
        inputPanel.Children.Add(InputBox);

        Grid.SetRow(inputPanel, 1);
        grid.Children.Add(inputPanel);

        Content = grid;

        AppendSystemMessage("NexTraceOne AI Chat ready. Ask about services, contracts or changes.");
    }

    private StackPanel MessagesPanel { get; set; } = null!;
    private TextBox InputBox { get; set; } = null!;

    private void OnSendClick(object sender, RoutedEventArgs e)
    {
        var text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputBox.Text = string.Empty;
        ThreadHelper.JoinableTaskFactory.RunAsync(() => SendQueryAsync(text));
    }

    private void OnInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter &&
            e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.None)
        {
            e.Handled = true;
            OnSendClick(sender, new RoutedEventArgs());
        }
    }

    private async Task SendQueryAsync(string query)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        AppendUserMessage(query);
        var options = GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            AppendSystemMessage("⚠ API key not configured. Go to Tools → Options → NexTraceOne → General.");
            return;
        }

        AppendSystemMessage("...");

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var response = await CallIdeQueryApiAsync(options, query, _cts.Token);
            RemoveLastSystemMessage();
            AppendAssistantMessage(response);
        }
        catch (OperationCanceledException)
        {
            RemoveLastSystemMessage();
        }
        catch (Exception ex)
        {
            RemoveLastSystemMessage();
            AppendSystemMessage($"❌ Error: {ex.Message}");
        }
    }

    private async Task<string> CallIdeQueryApiAsync(
        NexOptions options, string query, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            queryText = query,
            clientType = "visualstudio",
            clientVersion = "0.1.0",
            queryType = "GeneralQuery",
            persona = options.Persona
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(options.ServerUrl.TrimEnd('/') + "/api/v1/ai/ide/query"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {options.ApiKey}");
        request.Headers.Add("X-Client-Type", "visualstudio");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Server returned {(int)response.StatusCode}: {body}");

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(body);
            foreach (var key in new[] { "content", "output", "message", "response", "result" })
            {
                if (parsed.TryGetProperty(key, out var prop))
                    return prop.GetString() ?? body;
            }
        }
        catch { /* fallback to raw body */ }

        return body;
    }

    private void AppendUserMessage(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 4, 0, 4),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 380
        };
        MessagesPanel.Children.Add(tb);
        ScrollToBottom();
    }

    private void AppendAssistantMessage(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 4, 0, 4),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
            Foreground = Brushes.WhiteSmoke,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 440
        };
        MessagesPanel.Children.Add(tb);
        ScrollToBottom();
    }

    private void AppendSystemMessage(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 2, 0, 2),
            Foreground = Brushes.Gray,
            FontStyle = FontStyles.Italic,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Tag = "system"
        };
        MessagesPanel.Children.Add(tb);
        ScrollToBottom();
    }

    private void RemoveLastSystemMessage()
    {
        for (var i = MessagesPanel.Children.Count - 1; i >= 0; i--)
        {
            if (MessagesPanel.Children[i] is TextBlock tb && tb.Tag is "system" && tb.Text == "...")
            {
                MessagesPanel.Children.RemoveAt(i);
                return;
            }
        }
    }

    private void ScrollToBottom()
    {
        if (MessagesPanel.Parent is ScrollViewer sv)
            sv.ScrollToEnd();
    }

    private static NexOptions GetOptions()
    {
        var page = (NexTraceOneOptionsPage)Package.GetGlobalService(typeof(NexTraceOneOptionsPage));
        return page is not null
            ? new NexOptions(page.ServerUrl, page.ApiKey, page.Persona)
            : new NexOptions("http://localhost:5000", string.Empty, "Engineer");
    }

    private sealed record NexOptions(string ServerUrl, string ApiKey, string Persona);
}
