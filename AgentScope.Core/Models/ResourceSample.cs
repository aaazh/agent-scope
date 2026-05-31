namespace AgentScope.Core.Models;

/// <summary>
/// A resource usage sample for an AI tool's process tree.
/// </summary>
public class ResourceSample
{
    public string Tool { get; set; } = string.Empty;
    public int RootPid { get; set; }
    public double CpuPercent { get; set; }
    public double MemoryMb { get; set; }
    public int ProcessCount { get; set; }
    public List<ProcessInfo> Processes { get; set; } = new();
    public long Timestamp { get; set; }
}

/// <summary>
/// Resource info for a single process.
/// </summary>
public class ProcessInfo
{
    public int Pid { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CpuPercent { get; set; }
    public double MemoryMb { get; set; }
    public List<int> Children { get; set; } = new();
}
