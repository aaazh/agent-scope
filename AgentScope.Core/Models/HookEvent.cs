namespace AgentScope.Core.Models;

/// <summary>
/// Unified event types normalized across all supported AI tools.
/// </summary>
public enum EventType
{
    PreToolUse,
    PostToolUse,
    PostToolUseFailure,
    PermissionRequest,
    Stop,
    Notification,
    SessionStart,
    SessionEnd,
    SubagentStart,
    SubagentStop,
    UserPromptSubmit,
    PreCompact,
    PostCompact,
    ResourceSample,
    Status
}

/// <summary>
/// Event priority determines notification behavior.
/// </summary>
public enum EventPriority
{
    /// <summary>Silent update only — no notification or visual alert.</summary>
    Low,

    /// <summary>Compact bar visual indicator, no toast.</summary>
    Medium,

    /// <summary>Toast notification + auto-expand + visual alert.</summary>
    High
}

/// <summary>
/// A normalized hook event received from the bridge via Named Pipe.
/// </summary>
public class HookEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "hook_event";
    public string Event { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string Version { get; set; } = "1.0";

    public EventType EventType => Event switch
    {
        "PreToolUse" => Models.EventType.PreToolUse,
        "PostToolUse" => Models.EventType.PostToolUse,
        "PostToolUseFailure" => Models.EventType.PostToolUseFailure,
        "PermissionRequest" => Models.EventType.PermissionRequest,
        "Stop" => Models.EventType.Stop,
        "Notification" => Models.EventType.Notification,
        "SessionStart" => Models.EventType.SessionStart,
        "SessionEnd" => Models.EventType.SessionEnd,
        "SubagentStart" => Models.EventType.SubagentStart,
        "SubagentStop" => Models.EventType.SubagentStop,
        "UserPromptSubmit" => Models.EventType.UserPromptSubmit,
        "PreCompact" => Models.EventType.PreCompact,
        "PostCompact" => Models.EventType.PostCompact,
        "ResourceSample" => Models.EventType.ResourceSample,
        "Status" => Models.EventType.Status,
        _ => Models.EventType.Status
    };

    /// <summary>Get the priority for this event type.</summary>
    public EventPriority Priority => EventType switch
    {
        Models.EventType.PermissionRequest => EventPriority.High,
        Models.EventType.PostToolUseFailure => EventPriority.Medium,
        Models.EventType.SubagentStart => EventPriority.Medium,
        Models.EventType.SubagentStop => EventPriority.Medium,
        Models.EventType.Stop => EventPriority.Medium,
        Models.EventType.SessionEnd => EventPriority.Medium,
        Models.EventType.PreToolUse => EventPriority.Low,
        Models.EventType.PostToolUse => EventPriority.Low,
        Models.EventType.SessionStart => EventPriority.Low,
        Models.EventType.UserPromptSubmit => EventPriority.Low,
        Models.EventType.Notification => EventPriority.Low,
        Models.EventType.PreCompact => EventPriority.Low,
        Models.EventType.PostCompact => EventPriority.Low,
        Models.EventType.ResourceSample => EventPriority.Low,
        Models.EventType.Status => EventPriority.Low,
        _ => EventPriority.Low
    };
}
