using System.Text.Json;

namespace AgentScope.App;

/// <summary>
/// Simple JSON-backed application settings.
/// </summary>
public static class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AgentScope",
        "settings.json");

    private static SettingsData _data = new();

    public static double WindowLeft { get => _data.WindowLeft; set { _data.WindowLeft = value; Save(); } }
    public static double WindowTop { get => _data.WindowTop; set { _data.WindowTop = value; Save(); } }
    public static int WindowWidth { get => _data.WindowWidth; set { _data.WindowWidth = value; Save(); } }
    public static bool MinimizeToTray { get => _data.MinimizeToTray; set { _data.MinimizeToTray = value; Save(); } }
    public static int ExpandDelayMs { get => _data.ExpandDelayMs; set { _data.ExpandDelayMs = value; Save(); } }
    public static string Language { get => _data.Language; set { _data.Language = value; Save(); } }
    public static string DockEdge { get => _data.DockEdge; set { _data.DockEdge = value; Save(); } }
    public static double Opacity { get => _data.Opacity; set { _data.Opacity = value; Save(); } }
    public static bool EnableSound { get => _data.EnableSound; set { _data.EnableSound = value; Save(); } }
    public static bool EnableToast { get => _data.EnableToast; set { _data.EnableToast = value; Save(); } }

    static AppSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
            }
        }
        catch { /* use defaults */ }
    }

    /// <summary>Persist current settings to disk.</summary>
    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* best effort */ }
    }

    private class SettingsData
    {
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 0;
        public int WindowWidth { get; set; } = 320;
        public bool MinimizeToTray { get; set; } = true;
        public int ExpandDelayMs { get; set; } = 300;
        public string Language { get; set; } = "auto";
        public string DockEdge { get; set; } = "top";
        public double Opacity { get; set; } = 0.9;
        public bool EnableSound { get; set; } = true;
        public bool EnableToast { get; set; } = true;
    }
}
