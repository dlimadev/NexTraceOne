using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Simple input dialog used instead of Microsoft.VisualBasic.Interaction.InputBox.
/// </summary>
internal sealed partial class NexInputDialog : Window
{
    private readonly TextBox _inputBox;

    public NexInputDialog(string title, string prompt, string defaultValue)
    {
        Title = title;
        Width = 420;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        Foreground = Brushes.LightGray;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Margin = new Thickness(16);

        var promptBlock = new TextBlock
        {
            Text = prompt,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(promptBlock, 0);
        root.Children.Add(promptBlock);

        _inputBox = new TextBox
        {
            Text = defaultValue ?? string.Empty,
            Foreground = Brushes.LightGray,
            Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(_inputBox, 1);
        root.Children.Add(_inputBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0)
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 75,
            Height = 24,
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsDefault = true
        };
        okButton.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 75,
            Height = 24,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.LightGray,
            BorderThickness = new Thickness(0),
            IsCancel = true
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        Content = root;

        Loaded += (_, _) => _inputBox.Focus();
    }

    public string InputText => _inputBox.Text;
}
