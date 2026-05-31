namespace AgentScope.Core.Models;

/// <summary>
/// Current state of an AI tool session.
/// </summary>
public enum ToolStatus
{
    Idle,
    Running,
    WaitingPermission,
    Error,
    Disconnected
}

/// <summary>
/// Tracks the state of one AI tool session.
/// </summary>
public class ToolState
{
    public string ToolId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ToolStatus Status { get; set; } = ToolStatus.Disconnected;
    public string CurrentToolCall { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public int ToolCallCount { get; set; }
    public int SubagentCount { get; set; }
    public int PendingPermissionCount { get; set; }
    public List<string> RecentMessages { get; set; } = new();
    public List<SubagentInfo> ActiveSubagents { get; set; } = new();
    public TokenUsage? TokenUsage { get; set; }
    public ResourceSample? LatestResourceSample { get; set; }
    public TerminalInfo? Terminal { get; set; }
    public DateTimeOffset SessionStartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastEventTime { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Information about a running subagent.
/// </summary>
public class SubagentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public double ElapsedSeconds { get; set; }
}

/// <summary>
/// Information about the terminal session hosting the AI tool.
/// </summary>
public class TerminalInfo
{
    public string Type { get; set; } = string.Empty; // "WindowsTerminal", "PowerShell", "CMD"
    public string Title { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string? WindowId { get; set; } // WT window ID
    public int? TabIndex { get; set; }      // WT tab index
}

/// <summary>
/// An effect to be executed as a result of state reduction.
/// </summary>
public enum SideEffect
{
    None,
    PlaySound,
    SendToastNotification,
    FlashCompactBar,
    AutoExpand,
    AutoCollapse
}
