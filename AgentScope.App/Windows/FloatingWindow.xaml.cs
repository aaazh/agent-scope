using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AgentScope.App.Services;

namespace AgentScope.App.Windows;

public partial class FloatingWindow : Window
{
    // Win32 constants
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_TOPMOST = 0x8;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int smIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hwnd, int index, IntPtr newStyle);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    private readonly DockingService _dockingService;
    private readonly TrayService _trayService;
    private bool _isExpanded;
    private bool _isPinned;
    private bool _isDragging;

    public FloatingWindow()
    {
        InitializeComponent();
        _dockingService = new DockingService(this);
        _trayService = new TrayService(this);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var hwndSource = HwndSource.FromHwnd(hwnd);

        // Apply extended window styles for docked overlay behavior
        var exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);

        // Hook to prevent WPF from removing WS_EX_LAYERED
        hwndSource!.AddHook(WndProcHook);

        // Initialize from saved preferences
        LoadPreferences();
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_STYLECHANGING = 0x007D;

        if (msg == WM_STYLECHANGING && (long)wParam == GWL_EXSTYLE)
        {
            // Prevent WPF from stripping WS_EX_LAYERED
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (!_isDragging) return;
        _dockingService.CheckDocking();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        _isDragging = true;
        DragMove();
        _isDragging = false;
    }

    private void CompactBar_MouseEnter(object sender, MouseEventArgs e)
    {
        if (!_isExpanded && !_isPinned)
        {
            _ = ExpandAsync();
        }
    }

    private async Task ExpandAsync()
    {
        await Task.Delay(300); // Hover delay
        if (!IsMouseOver && !_isPinned) return;

        Dispatcher.Invoke(() =>
        {
            _isExpanded = true;
            ExpandedPanel.Visibility = Visibility.Visible;
            ExpandedRow.Height = new GridLength(1, GridUnitType.Star);
            MinHeight = 200;
            Height = Math.Max(Height, 350);
        });
    }

    private void CompactBar_MouseLeave(object sender, MouseEventArgs e)
    {
        _ = CollapseAfterDelay();
    }

    private async Task CollapseAfterDelay()
    {
        await Task.Delay(500); // Collapse delay
        if (_isPinned) return;

        Dispatcher.Invoke(() =>
        {
            if (!IsMouseOver)
            {
                Collapse();
            }
        });
    }

    private void Collapse()
    {
        _isExpanded = false;
        ExpandedPanel.Visibility = Visibility.Collapsed;
        ExpandedRow.Height = new GridLength(0);
        MinHeight = 28;
        Height = Math.Max(56, ToolsList.Items.Count * 32 + 16);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        PinButton.Content = _isPinned ? "📍" : "📌";
    }

    /// <summary>Update the compact bar tool rows from session state.</summary>
    public void UpdateToolRows(/* SessionSnapshot state */)
    {
        // In production: bind to ViewModel which observes state changes
        // For now: placeholder that would be replaced with proper data binding
    }

    /// <summary>Flash the compact bar border (permission alert).</summary>
    public void FlashAlert()
    {
        var originalBrush = MainBorder.BorderBrush;
        MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)); // DangerBrush
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = originalBrush);
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)));
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = originalBrush);
        });
    }

    private void LoadPreferences()
    {
        // Restore saved position and dock preference
        Left = Properties.Settings.Default.WindowLeft;
        Top = Properties.Settings.Default.WindowTop;
        Width = Properties.Settings.Default.WindowWidth > 0
            ? Properties.Settings.Default.WindowWidth : 320;
    }

    protected override void OnClosed(EventArgs e)
    {
        // Save preferences
        Properties.Settings.Default.WindowLeft = Left;
        Properties.Settings.Default.WindowTop = Top;
        Properties.Settings.Default.WindowWidth = Width;
        Properties.Settings.Default.Save();

        _trayService.Dispose();
        base.OnClosed(e);
    }
}
