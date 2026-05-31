using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AgentScope.App.Controls;

public partial class TokenGauge : UserControl
{
    public static readonly DependencyProperty TotalTokensProperty =
        DependencyProperty.Register(nameof(TotalTokens), typeof(int), typeof(TokenGauge),
            new PropertyMetadata(0, OnTokenChanged));

    public static readonly DependencyProperty TokenLimitProperty =
        DependencyProperty.Register(nameof(TokenLimit), typeof(int), typeof(TokenGauge),
            new PropertyMetadata(200000, OnTokenChanged));

    public static readonly DependencyProperty IsEstimatedProperty =
        DependencyProperty.Register(nameof(IsEstimated), typeof(bool), typeof(TokenGauge),
            new PropertyMetadata(false));

    public int TotalTokens
    {
        get => (int)GetValue(TotalTokensProperty);
        set => SetValue(TotalTokensProperty, value);
    }

    public int TokenLimit
    {
        get => (int)GetValue(TokenLimitProperty);
        set => SetValue(TokenLimitProperty, value);
    }

    public bool IsEstimated
    {
        get => (bool)GetValue(IsEstimatedProperty);
        set => SetValue(IsEstimatedProperty, value);
    }

    public TokenGauge()
    {
        InitializeComponent();
    }

    private static void OnTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (TokenGauge)d;
        gauge.UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var percent = TokenLimit > 0
            ? Math.Min((double)TotalTokens / TokenLimit * 100.0, 100.0)
            : 0.0;

        var maxWidth = ActualWidth > 0 ? ActualWidth : 300;
        ProgressFill.Width = maxWidth * percent / 100.0;

        // Format numbers nicely
        var usedStr = TotalTokens >= 1000 ? $"{TotalTokens / 1000.0:F1}K" : $"{TotalTokens}";
        var limitStr = TokenLimit >= 1000 ? $"{TokenLimit / 1000.0:F0}K" : $"{TokenLimit}";

        TokenLabel.Text = $"Token: {usedStr} / {limitStr}";
        PercentLabel.Text = IsEstimated
            ? $"{percent:F1}% (估算)"
            : $"{percent:F1}%";

        // Color coding
        if (percent >= 95)
        {
            ProgressFill.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)); // Red
        }
        else if (percent >= 80)
        {
            ProgressFill.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x02)); // Yellow/Orange
        }
        else
        {
            ProgressFill.Background = new SolidColorBrush(Color.FromRgb(0x6C, 0x63, 0xFF)); // Accent purple
        }
    }
}
