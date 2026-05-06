using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    private void Log_Click(object sender, RoutedEventArgs e)
    {
        var logTextBox = new TextBox
        {
            Text = LogTextBox.Text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            IsReadOnly = true,
            TextWrapping = TextWrapping.NoWrap,
            AcceptsReturn = true,
            AcceptsTab = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Padding = new Thickness(8)
        };

        var copyButton = new Button
        {
            Content = "Copy",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0)
        };

        copyButton.Click += (_, _) =>
        {
            Clipboard.SetText(logTextBox.Text ?? "");
        };

        var clearButton = new Button
        {
            Content = "Clear",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0)
        };

        clearButton.Click += (_, _) =>
        {
            LogTextBox.Clear();
            logTextBox.Clear();
        };

        var closeButton = new Button
        {
            Content = "Close",
            Width = 90
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 8)
        };

        buttonPanel.Children.Add(copyButton);
        buttonPanel.Children.Add(clearButton);
        buttonPanel.Children.Add(closeButton);

        var layout = new Grid
        {
            Margin = new Thickness(12)
        };

        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(buttonPanel, 0);
        Grid.SetRow(logTextBox, 1);

        layout.Children.Add(buttonPanel);
        layout.Children.Add(logTextBox);

        var window = new Window
        {
            Title = "Diagnostics Log",
            Owner = this,
            Width = 1100,
            Height = 720,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
            Content = layout
        };

        closeButton.Click += (_, _) => window.Close();

        window.ShowDialog();
    }
}
