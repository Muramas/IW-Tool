using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    private void SuggestedRecommendation_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is null)
        {
            return;
        }

        var card = element.DataContext;

        var slot = ReadRecommendationProperty(card, "SlotDisplay");
        var current = ReadRecommendationProperty(card, "ItemDisplay");
        var enchantContext = ReadRecommendationProperty(card, "CurrentEnchantContext");

        var suggested = ReadRecommendationProperty(card, "SuggestedItemDisplay");
        var rank = ReadRecommendationProperty(card, "SuggestedRankDisplay");
        var gain = ReadRecommendationProperty(card, "SuggestedGainDisplay");
        var reason = ReadRecommendationProperty(card, "SuggestedReason");
        var status = ReadRecommendationProperty(card, "SuggestedStatus");

        var window = new Window
        {
            Title = "Recommendations - " + slot,
            Owner = this,
            Width = 1100,
            Height = 760,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18))
        };

        var root = new Grid
        {
            Margin = new Thickness(14)
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = slot,
            Foreground = Brushes.White,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };

        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var summary = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var currentCard = BuildRecommendationSummaryCard(
            "Current",
            current,
            enchantContext,
            "This is the item currently configured in the scenario."
        );

        var suggestedCard = BuildRecommendationSummaryCard(
            "Suggested",
            suggested,
            FirstNonEmpty(status, rank, gain),
            FirstNonEmpty(reason, "No recommendation explanation is available yet.")
        );

        Grid.SetColumn(currentCard, 0);
        Grid.SetColumn(suggestedCard, 2);

        summary.Children.Add(currentCard);
        summary.Children.Add(suggestedCard);

        Grid.SetRow(summary, 1);
        root.Children.Add(summary);

        var tabs = new TabControl
        {
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70))
        };

        var explanationText = new TextBox
        {
            Text = BuildExplanationText(slot, current, enchantContext, suggested, rank, gain, reason),
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Padding = new Thickness(8)
        };

        tabs.Items.Add(new TabItem
        {
            Header = "Why this item",
            Content = explanationText
        });

        var rankedGrid = new DataGrid
        {
            AutoGenerateColumns = true,
            IsReadOnly = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            RowBackground = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            ItemsSource = ResultsGrid.ItemsSource
        };

        tabs.Items.Add(new TabItem
        {
            Header = "Ranked list",
            Content = rankedGrid
        });

        Grid.SetRow(tabs, 2);
        root.Children.Add(tabs);

        var footer = new DockPanel
        {
            Margin = new Thickness(0, 12, 0, 0)
        };

        var note = new TextBlock
        {
            Text = "Next: make this list slot-specific and enchant-aware.",
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var closeButton = new Button
        {
            Content = "Close",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        closeButton.Click += (_, _) => window.Close();

        DockPanel.SetDock(closeButton, Dock.Right);

        footer.Children.Add(closeButton);
        footer.Children.Add(note);

        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        window.Content = root;
        window.ShowDialog();

        e.Handled = true;
    }

    private static string BuildExplanationText(
        string slot,
        string current,
        string enchantContext,
        string suggested,
        string rank,
        string gain,
        string reason)
    {
        return
            "Slot" + Environment.NewLine +
            "----" + Environment.NewLine +
            slot + Environment.NewLine +
            Environment.NewLine +
            "Current item" + Environment.NewLine +
            "------------" + Environment.NewLine +
            current + Environment.NewLine +
            enchantContext + Environment.NewLine +
            Environment.NewLine +
            "Suggested item" + Environment.NewLine +
            "--------------" + Environment.NewLine +
            FirstNonEmpty(suggested, "No suggestion available") + Environment.NewLine +
            FirstNonEmpty(rank, "") + Environment.NewLine +
            FirstNonEmpty(gain, "") + Environment.NewLine +
            Environment.NewLine +
            "Reason" + Environment.NewLine +
            "------" + Environment.NewLine +
            FirstNonEmpty(reason, "No recommendation explanation is available yet.") + Environment.NewLine +
            Environment.NewLine +
            "Important" + Environment.NewLine +
            "---------" + Environment.NewLine +
            "Recommendations should be recalculated when enchant assumptions change. " +
            "The best item at Enchant 1+5 may differ from the best item at Enchant 25+5.";
    }

    private static Border BuildRecommendationSummaryCard(
        string title,
        string main,
        string sub,
        string note)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 40, 56)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 106, 144)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10)
        };

        var stack = new StackPanel();

        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        stack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(main) ? "None" : main,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        stack.Children.Add(new TextBlock
        {
            Text = sub ?? "",
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        stack.Children.Add(new TextBlock
        {
            Text = note ?? "",
            Foreground = new SolidColorBrush(Color.FromRgb(205, 205, 205)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        border.Child = stack;

        return border;
    }

    private static string ReadRecommendationProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        var value = property?.GetValue(source);

        return value?.ToString() ?? "";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }
}
