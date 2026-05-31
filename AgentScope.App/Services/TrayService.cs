using System.Windows;

namespace AgentScope.App.Services;

/// <summary>
/// System tray integration for AgentScope.
/// </summary>
public class TrayService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;

    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem("显示/隐藏", null, OnToggleVisibility),
            new ToolStripMenuItem("设置", null, OnOpenSettings),
            new ToolStripSeparator(),
            new ToolStripMenuItem("退出", null, OnExit)
        });

        // Use a simple icon — in production, load from embedded resource
        _notifyIcon = new NotifyIcon
        {
            Text = "AgentScope",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        // Try to load the app icon; fallback to system icon
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "app.ico");
            if (File.Exists(iconPath))
                _notifyIcon.Icon = new Icon(iconPath);
            else
                _notifyIcon.Icon = SystemIcons.Application;
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }

        _notifyIcon.DoubleClick += OnToggleVisibility;
    }

    private void OnToggleVisibility(object? sender, EventArgs e)
    {
        if (_mainWindow.Visibility == Visibility.Visible)
            _mainWindow.Hide();
        else
            _mainWindow.Show();
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        // Settings window will be created later
        System.Windows.MessageBox.Show("Settings window coming soon.", "AgentScope");
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _mainWindow.Close();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
