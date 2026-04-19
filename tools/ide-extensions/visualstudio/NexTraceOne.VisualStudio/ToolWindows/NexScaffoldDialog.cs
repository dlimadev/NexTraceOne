using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexTraceOne.VisualStudio.Commands;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Diálogo WPF de scaffolding de serviço no Visual Studio.
/// Apresenta listagem de templates com filtro, campos de nome, equipa, domínio
/// e directório de saída. O utilizador confirma para iniciar a geração.
/// </summary>
internal sealed class NexScaffoldDialog : Window
{
    private readonly NexAiScaffoldCommand.TemplateSummary[] _templates;

    private ListBox _templateList = null!;
    private TextBox _serviceNameBox = null!;
    private TextBox _teamBox = null!;
    private TextBox _domainBox = null!;
    private TextBox _outputDirBox = null!;
    private CheckBox _openAfterCheckBox = null!;
    private TextBlock _statusText = null!;

    public NexAiScaffoldCommand.TemplateSummary SelectedTemplate { get; private set; } = null!;
    public string ServiceName { get; private set; } = string.Empty;
    public string? TeamName { get; private set; }
    public string? Domain { get; private set; }
    public string OutputDirectory { get; private set; } = string.Empty;
    public bool OpenAfterScaffolding { get; private set; }

    public NexScaffoldDialog(NexAiScaffoldCommand.TemplateSummary[] templates, string? solutionDir)
    {
        _templates = templates ?? [];
        Title = "NexTraceOne — Scaffold New Service";
        Width = 580;
        Height = 560;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        Background = new SolidColorBrush(Color.FromRgb(37, 37, 38));
        Foreground = Brushes.WhiteSmoke;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildLayout(solutionDir);
    }

    private void BuildLayout(string? solutionDir)
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // title
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // template search
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(160) }); // template list
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // form
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // status
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // buttons

        // Title
        var title = new TextBlock
        {
            Text = "🚀 Scaffold New Service from NexTraceOne Template",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.WhiteSmoke,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        // Template search
        var searchBox = new TextBox
        {
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 4)
        };
        searchBox.SetValue(TextBox.AcceptsReturnProperty, false);
        var searchLabel = new TextBlock { Text = "Filter templates:", Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) };
        var searchPanel = new StackPanel();
        searchPanel.Children.Add(searchLabel);
        searchPanel.Children.Add(searchBox);
        Grid.SetRow(searchPanel, 1);
        root.Children.Add(searchPanel);

        // Template list
        _templateList = new ListBox
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        PopulateTemplateList(string.Empty);
        _templateList.SelectionChanged += OnTemplateSelected;
        Grid.SetRow(_templateList, 2);
        root.Children.Add(_templateList);

        searchBox.TextChanged += (_, _) => PopulateTemplateList(searchBox.Text);

        // Form fields
        var form = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 5; i++)
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _serviceNameBox = AddFormRow(form, 0, "Service name*:", "e.g. payment-service");
        _teamBox = AddFormRow(form, 1, "Team:", "e.g. platform-team");
        _domainBox = AddFormRow(form, 2, "Domain:", "e.g. payments");
        _outputDirBox = AddFormRow(form, 3, "Output folder:", solutionDir ?? ".");

        _openAfterCheckBox = new CheckBox
        {
            Content = "Open project after scaffolding",
            Foreground = Brushes.LightGray,
            Margin = new Thickness(4, 6, 0, 0),
            IsChecked = true
        };
        Grid.SetRow(_openAfterCheckBox, 4);
        Grid.SetColumn(_openAfterCheckBox, 1);
        form.Children.Add(_openAfterCheckBox);

        var browseBtn = new Button
        {
            Content = "…",
            Width = 28,
            Height = 22,
            Margin = new Thickness(4, 4, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.LightGray,
            BorderThickness = new Thickness(0)
        };
        browseBtn.Click += (_, _) =>
        {
            // Use simple InputBox-style prompt since WinForms may not be available in VSIX
            var dlg = new Window
            {
                Title = "Select Output Directory",
                Width = 500,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38))
            };
            var dlgPanel = new StackPanel { Margin = new Thickness(12) };
            dlgPanel.Children.Add(new TextBlock { Text = "Output directory path:", Foreground = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 6) });
            var pathBox = new TextBox
            {
                Text = _outputDirBox.Text,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.WhiteSmoke,
                Padding = new Thickness(4)
            };
            dlgPanel.Children.Add(pathBox);
            var okBtn = new Button { Content = "OK", Width = 60, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            okBtn.Click += (_, _) => { _outputDirBox.Text = pathBox.Text; dlg.DialogResult = true; dlg.Close(); };
            dlgPanel.Children.Add(okBtn);
            dlg.Content = dlgPanel;
            dlg.ShowDialog();
        };
        var outputPanel = new DockPanel();
        DockPanel.SetDock(browseBtn, Dock.Right);
        outputPanel.Children.Add(browseBtn);
        outputPanel.Children.Add(_outputDirBox);
        Grid.SetRow(outputPanel, 3);
        Grid.SetColumn(outputPanel, 1);
        // Replace the plain TextBox with the panel
        form.Children.Remove(_outputDirBox);
        form.Children.Add(outputPanel);

        Grid.SetRow(form, 3);
        root.Children.Add(form);

        // Status
        _statusText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 70)),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
            Visibility = Visibility.Collapsed
        };
        Grid.SetRow(_statusText, 4);
        root.Children.Add(_statusText);

        // Buttons
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };
        var scaffoldBtn = new Button
        {
            Content = "Scaffold →",
            Width = 100,
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        scaffoldBtn.Click += OnScaffoldClick;
        btnPanel.Children.Add(cancelBtn);
        btnPanel.Children.Add(scaffoldBtn);
        Grid.SetRow(btnPanel, 5);
        root.Children.Add(btnPanel);

        Content = root;
    }

    private void PopulateTemplateList(string filter)
    {
        _templateList.Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _templates
            : _templates.Where(t =>
                (t.DisplayName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.ServiceType?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Language?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)).ToArray();

        foreach (var t in filtered)
        {
            var item = new ListBoxItem
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = $"{t.DisplayName} v{t.Version}", FontWeight = FontWeights.SemiBold, FontSize = 11 },
                        new TextBlock
                        {
                            Text = $"{t.ServiceType} · {t.Language}{(t.HasBaseContract ? " · Contract" : string.Empty)}",
                            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                            FontSize = 10
                        }
                    }
                },
                Tag = t
            };
            _templateList.Items.Add(item);
        }
    }

    private void OnTemplateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_templateList.SelectedItem is ListBoxItem { Tag: NexAiScaffoldCommand.TemplateSummary t })
        {
            _teamBox.Text = t.DefaultTeam;
            _domainBox.Text = t.DefaultDomain;
        }
    }

    private void OnScaffoldClick(object sender, RoutedEventArgs e)
    {
        // Validate
        if (_templateList.SelectedItem is not ListBoxItem { Tag: NexAiScaffoldCommand.TemplateSummary selectedTemplate })
        {
            ShowStatus("Please select a template.");
            return;
        }

        var serviceName = _serviceNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(serviceName) || !System.Text.RegularExpressions.Regex.IsMatch(serviceName, @"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$"))
        {
            ShowStatus("Service name must be lowercase kebab-case (e.g. payment-service).");
            return;
        }

        var outputDir = _outputDirBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            ShowStatus("Output folder is required.");
            return;
        }

        // Build full output path
        if (!Path.IsPathRooted(outputDir))
            outputDir = Path.GetFullPath(outputDir);
        outputDir = Path.Combine(outputDir, serviceName);

        SelectedTemplate = selectedTemplate;
        ServiceName = serviceName;
        TeamName = string.IsNullOrWhiteSpace(_teamBox.Text) ? null : _teamBox.Text.Trim();
        Domain = string.IsNullOrWhiteSpace(_domainBox.Text) ? null : _domainBox.Text.Trim();
        OutputDirectory = outputDir;
        OpenAfterScaffolding = _openAfterCheckBox.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void ShowStatus(string message)
    {
        _statusText.Text = message;
        _statusText.Visibility = Visibility.Visible;
    }

    private static TextBox AddFormRow(Grid grid, int row, string label, string placeholder)
    {
        var lbl = new TextBlock
        {
            Text = label,
            Foreground = Brushes.LightGray,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 8, 4)
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, 0);
        grid.Children.Add(lbl);

        var tb = new TextBox
        {
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 4, 0, 4)
        };
        Grid.SetRow(tb, row);
        Grid.SetColumn(tb, 1);
        grid.Children.Add(tb);

        return tb;
    }
}
