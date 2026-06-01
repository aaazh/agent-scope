using System.Windows;
using Microsoft.Win32;

namespace AgentScope.App.Windows;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // General
        StartWithWindowsCheck.IsChecked = IsStartWithWindowsEnabled();
        MinimizeToTrayCheck.IsChecked = AppSettings.MinimizeToTray;

        // Appearance
        WidthBox.Text = AppSettings.WindowWidth > 0
            ? AppSettings.WindowWidth.ToString() : "320";
        ExpandDelayBox.Text = AppSettings.ExpandDelayMs > 0
            ? AppSettings.ExpandDelayMs.ToString() : "300";

        // Language detection
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        if (culture.Name.StartsWith("zh"))
            LanguageCombo.SelectedIndex = 0;
        else
            LanguageCombo.SelectedIndex = 1;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save start with Windows
        SetStartWithWindows(StartWithWindowsCheck.IsChecked == true);

        // Save appearance
        if (int.TryParse(WidthBox.Text, out int width) && width >= 200 && width <= 600)
            AppSettings.WindowWidth = width;
        if (int.TryParse(ExpandDelayBox.Text, out int delay) && delay >= 100 && delay <= 2000)
            AppSettings.ExpandDelayMs = delay;

        AppSettings.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
        AppSettings.Save();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static bool IsStartWithWindowsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("AgentScope") != null;
        }
        catch
        {
            return false;
        }
    }

    private static void SetStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
            {
                var exePath = Environment.ProcessPath ?? "AgentScope.exe";
                key?.SetValue("AgentScope", $"\"{exePath}\"");
            }
            else
            {
                key?.DeleteValue("AgentScope", false);
            }
        }
        catch
        {
            // Registry access denied — silently ignore
        }
    }
}
