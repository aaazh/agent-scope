using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AgentScope.App.Services;
using AgentScope.App.ViewModels;

namespace AgentScope.App.Windows;

public partial class FloatingWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TOPMOST = 0x8;
    private const int WS_EX_TOOLWINDOW = 0x80;

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hwnd, int index, IntPtr newStyle);

    private DockingService _dockingService = null!;
    private TrayService _trayService = null!;
    private MainViewModel? _viewModel;
    private bool _isDragging;

    public FloatingWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);

        var hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource!.AddHook(WndProcHook);

        _dockingService = new DockingService(this);
        _trayService = new TrayService(this);

        // Start MVVM data flow
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.OnFlashRequested += FlashAlert;

        LoadPreferences();
        await _viewModel.StartAsync();
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x007D /* WM_STYLECHANGING */ && (long)wParam == GWL_EXSTYLE)
        {
            handled = true;
            return IntPtr.Zero;
        }
        return IntPtr.Zero;
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (_isDragging) _dockingService.CheckDocking();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        _isDragging = true;
        DragMove();
        _isDragging = false;
    }

    private async void CompactBar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel is { IsExpanded: false, IsPinned: false })
        {
            await Task.Delay(300);
            if (IsMouseOver && _viewModel is { IsPinned: false })
                _viewModel.Expand();
        }
        UpdateExpandedPanel();
    }

    private async void CompactBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        await Task.Delay(500);
        if (_viewModel is { IsPinned: false } && !IsMouseOver)
        {
            _viewModel.Collapse();
            UpdateExpandedPanel();
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e) => _viewModel?.TogglePin();

    private void UpdateExpandedPanel()
    {
        if (_viewModel?.IsExpanded == true)
        {
            ExpandedPanel.Visibility = Visibility.Visible;
            ExpandedRow.Height = new GridLength(1, GridUnitType.Star);
            MinHeight = 200;
            Height = Math.Max(Height, 350);
        }
        else
        {
            ExpandedPanel.Visibility = Visibility.Collapsed;
            ExpandedRow.Height = new GridLength(0);
            MinHeight = 28;
        }
    }

    public void FlashAlert()
    {
        var orig = MainBorder.BorderBrush;
        MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57));
        _ = Task.Run(async () => {
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = orig);
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)));
            await Task.Delay(200);
            Dispatcher.Invoke(() => MainBorder.BorderBrush = orig);
        });
    }

    private void LoadPreferences()
    {
        Left = AppSettings.WindowLeft;
        Top = AppSettings.WindowTop;
        Width = AppSettings.WindowWidth > 0 ? AppSettings.WindowWidth : 320;
    }

    protected override void OnClosed(EventArgs e)
    {
        AppSettings.WindowLeft = Left;
        AppSettings.WindowTop = Top;
        AppSettings.WindowWidth = (int)Width;
        AppSettings.Save();
        _viewModel?.Dispose();
        _trayService.Dispose();
        base.OnClosed(e);
    }
}
