namespace AgentScope.Core.Models;

/// <summary>
/// A complete snapshot of all AI tool sessions and their state.
/// This is the "truth layer" — all state flows through this.
/// </summary>
public class SessionSnapshot
{
    public List<ToolState> Tools { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public bool IsAllIdle => Tools.TrueForAll(t => t.Status == ToolStatus.Idle || t.Status == ToolStatus.Disconnected);
    public int TotalPendingPermissions => Tools.Sum(t => t.PendingPermissionCount);

    /// <summary>Total CPU usage across all tools.</summary>
    public double TotalCpuPercent => Tools
        .Where(t => t.LatestResourceSample != null)
        .Sum(t => t.LatestResourceSample!.CpuPercent);

    /// <summary>Total memory usage across all tools (MB).</summary>
    public double TotalMemoryMb => Tools
        .Where(t => t.LatestResourceSample != null)
        .Sum(t => t.LatestResourceSample!.MemoryMb);

    /// <summary>Total token usage across all tools.</summary>
    public int TotalTokens => Tools
        .Where(t => t.TokenUsage != null)
        .Sum(t => t.TokenUsage!.TotalTokens);
}
