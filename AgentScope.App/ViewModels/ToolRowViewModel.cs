using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AgentScope.App.ViewModels;

/// <summary>
/// ViewModel for one AI tool row in the compact bar.
/// Bound to FloatingWindow via ItemsControl + DataTemplate.
/// </summary>
public class ToolRowViewModel : INotifyPropertyChanged
{
    private string _toolId = "";
    private string _displayName = "";
    private string _status = "idle";
    private string _currentActivity = "";
    private double _cpuPercent;
    private double _memoryMb;
    private int _pendingPermissions;
    private int _totalTokens;
    private int _tokenLimit = 200000;

    public string ToolId { get => _toolId; set => SetField(ref _toolId, value); }
    public string DisplayName { get => _displayName; set => SetField(ref _displayName, value); }
    public string Status { get => _status; set => SetField(ref _status, value); }
    public string CurrentActivity { get => _currentActivity; set => SetField(ref _currentActivity, value); }
    public double CpuPercent { get => _cpuPercent; set => SetField(ref _cpuPercent, value); }
    public double MemoryMb { get => _memoryMb; set => SetField(ref _memoryMb, value); }
    public int PendingPermissions { get => _pendingPermissions; set => SetField(ref _pendingPermissions, value); }
    public int TotalTokens { get => _totalTokens; set => SetField(ref _totalTokens, value); }
    public int TokenLimit { get => _tokenLimit; set => SetField(ref _tokenLimit, value); }

    public System.Windows.Media.Brush StatusBrush => Status switch
    {
        "running" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0xD5, 0x73)),
        "waiting_permission" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x47, 0x57)),
        "error" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x47, 0x57)),
        _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA0, 0xA0, 0xB0))
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
