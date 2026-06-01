using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AgentScope.Core.Models;
using AgentScope.Core.Pipe;
using AgentScope.Core.State;

namespace AgentScope.App.ViewModels;

/// <summary>
/// Root ViewModel — owns the full data flow:
/// NamedPipeClient → Reducer → ObservableCollection → UI binding.
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly NamedPipeClient _pipeClient;
    private readonly string? _bridgeExePath;
    private Process? _bridgeProcess;
    private SessionSnapshot _snapshot = new();

    public ObservableCollection<ToolRowViewModel> Tools { get; } = new();

    private double _totalCpuPercent;
    public double TotalCpuPercent { get => _totalCpuPercent; set => SetField(ref _totalCpuPercent, value); }

    private double _totalMemoryMb;
    public double TotalMemoryMb { get => _totalMemoryMb; set => SetField(ref _totalMemoryMb, value); }

    private bool _isExpanded;
    public bool IsExpanded { get => _isExpanded; set => SetField(ref _isExpanded, value); }

    private bool _isPinned;
    public bool IsPinned { get => _isPinned; set => SetField(ref _isPinned, value); }

    private ToolRowViewModel? _selectedTool;
    public ToolRowViewModel? SelectedTool { get => _selectedTool; set => SetField(ref _selectedTool, value); }

    private string _connectionStatus = "disconnected";
    public string ConnectionStatus { get => _connectionStatus; set => SetField(ref _connectionStatus, value); }

    public MainViewModel()
    {
        _bridgeExePath = FindBridgeExe();
        _pipeClient = new NamedPipeClient();
        _pipeClient.OnHookEvent += OnHookEvent;
        _pipeClient.OnStatusChanged += OnPipeStatusChanged;
    }

    /// <summary>Start bridge + begin listening for events.</summary>
    public async Task StartAsync()
    {
        StartBridge();
        _ = Task.Run(() => _pipeClient.ConnectAsync());
    }

    /// <summary>Handle incoming hook event from Named Pipe.</summary>
    private void OnHookEvent(HookEvent hookEvent)
    {
        // Pass through the pure reducer
        var (newState, effects) = Reducer.Reduce(_snapshot, hookEvent);
        _snapshot = newState;

        // Rebuild ViewModel collection from snapshot
        System.Windows.Application.Current?.Dispatcher.Invoke(() => RebuildToolRows());

        // Handle side effects
        foreach (var effect in effects) {
            switch (effect) {
                case SideEffect.FlashCompactBar:
                    OnFlashRequested?.Invoke();
                    break;
                case SideEffect.AutoExpand:
                    IsExpanded = true;
                    break;
            }
        }
    }

    /// <summary>Send a permission decision back to the bridge.</summary>
    public async Task SendPermissionDecision(string eventId, bool allow) {
        await _pipeClient.SendPermissionDecision(eventId, allow);
    }

    /// <summary>Rebuild the Tools collection from current SessionSnapshot.</summary>
    private void RebuildToolRows()
    {
        // Sync existing rows and add new ones
        foreach (var tool in _snapshot.Tools) {
            var existing = Tools.FirstOrDefault(t => t.ToolId == tool.ToolId);
            if (existing == null) {
                existing = new ToolRowViewModel { ToolId = tool.ToolId, DisplayName = tool.Name };
                Tools.Add(existing);
                if (SelectedTool == null) SelectedTool = existing;
            }
            existing.Status = MapStatus(tool.Status);
            existing.CurrentActivity = tool.CurrentToolCall;
            existing.PendingPermissions = tool.PendingPermissionCount;
            if (tool.LatestResourceSample != null) {
                existing.CpuPercent = tool.LatestResourceSample.CpuPercent;
                existing.MemoryMb = tool.LatestResourceSample.MemoryMb;
            }
            if (tool.TokenUsage != null) {
                existing.TotalTokens = tool.TokenUsage.TotalTokens;
                existing.TokenLimit = tool.TokenUsage.Limit ?? 200000;
            }
        }

        // Remove disconnected tools
        var activeIds = _snapshot.Tools.Select(t => t.ToolId).ToHashSet();
        for (int i = Tools.Count - 1; i >= 0; i--)
            if (!activeIds.Contains(Tools[i].ToolId))
                Tools.RemoveAt(i);

        TotalCpuPercent = _snapshot.TotalCpuPercent;
        TotalMemoryMb = _snapshot.TotalMemoryMb;

        if (_snapshot.TotalPendingPermissions > 0)
            OnPermissionAlert?.Invoke(_snapshot.TotalPendingPermissions);
    }

    private static string MapStatus(AgentScope.Core.Models.ToolStatus status) => status switch {
        AgentScope.Core.Models.ToolStatus.Running => "running",
        AgentScope.Core.Models.ToolStatus.WaitingPermission => "waiting_permission",
        AgentScope.Core.Models.ToolStatus.Error => "error",
        _ => "idle"
    };

    /// <summary>Expand the floating window panel.</summary>
    public void Expand() => IsExpanded = true;
    public void Collapse() { if (!IsPinned) IsExpanded = false; }
    public void TogglePin() => IsPinned = !IsPinned;

    /// <summary>Events for UI coordination (not tied to data binding).</summary>
    public event Action? OnFlashRequested;
    public event Action<int>? OnPermissionAlert;

    // ── Bridge lifecycle ──

    private string? FindBridgeExe() {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var bridgePath = Path.Combine(exeDir, "agent-hooks-bridge.exe");
        return File.Exists(bridgePath) ? bridgePath : null;
    }

    private void StartBridge() {
        if (_bridgeExePath == null) return;
        _bridgeProcess = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = _bridgeExePath,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };
        _bridgeProcess.Start();
    }

    private void OnPipeStatusChanged(string status) {
        ConnectionStatus = status;
    }

    public void Dispose() {
        _pipeClient.OnHookEvent -= OnHookEvent;
        _pipeClient.Dispose();
        _bridgeProcess?.Kill();
        _bridgeProcess?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── INotifyPropertyChanged ──
    public event PropertyChangedEventHandler? PropertyChanged;
    void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
