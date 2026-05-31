namespace AgentScope.Core.Models;

/// <summary>
/// Token usage data for an AI tool session.
/// </summary>
public class TokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public int? Limit { get; set; }
    public double UsagePercent => Limit.HasValue && Limit > 0
        ? (double)TotalTokens / Limit.Value * 100.0
        : 0.0;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public bool IsEstimated { get; set; }
}
