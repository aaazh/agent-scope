using AgentScope.Core.Models;

namespace AgentScope.Core.State;

/// <summary>
/// Pure functional reducer: (state, event) → (newState, effects).
/// No side effects, no mutation — fully testable.
/// Pattern inspired by CodeIsland's reduceEvent().
/// </summary>
public static class Reducer
{
    /// <summary>
    /// Reduce a single hook event into a new session snapshot and list of side effects.
    /// </summary>
    public static (SessionSnapshot NewState, List<SideEffect> Effects) Reduce(
        SessionSnapshot currentState,
        HookEvent hookEvent)
    {
        var state = Clone(currentState);
        var effects = new List<SideEffect>();

        // Find or create the tool state entry
        var tool = state.Tools.Find(t => t.ToolId == hookEvent.Tool);
        if (tool == null)
        {
            tool = new ToolState
            {
                ToolId = hookEvent.Tool,
                Name = GetToolDisplayName(hookEvent.Tool),
                Status = ToolStatus.Running,
                SessionStartTime = DateTimeOffset.FromUnixTimeMilliseconds(hookEvent.Timestamp)
            };
            state.Tools.Add(tool);
        }

        tool.LastEventTime = DateTimeOffset.FromUnixTimeMilliseconds(hookEvent.Timestamp);
        state.LastUpdated = tool.LastEventTime;

        // Apply event-specific state transitions
        switch (hookEvent.EventType)
        {
            case EventType.SessionStart:
                tool.Status = ToolStatus.Running;
                tool.SessionStartTime = DateTimeOffset.FromUnixTimeMilliseconds(hookEvent.Timestamp);
                effects.Add(SideEffect.PlaySound);
                break;

            case EventType.SessionEnd:
                tool.Status = ToolStatus.Idle;
                break;

            case EventType.PreToolUse:
                tool.Status = ToolStatus.Running;
                tool.CurrentToolCall = hookEvent.DataJson;
                tool.ToolCallCount++;
                break;

            case EventType.PostToolUse:
                tool.Status = ToolStatus.Running;
                tool.LastActivity = hookEvent.DataJson;
                break;

            case EventType.PostToolUseFailure:
                tool.Status = ToolStatus.Error;
                effects.Add(SideEffect.FlashCompactBar);
                break;

            case EventType.PermissionRequest:
                tool.Status = ToolStatus.WaitingPermission;
                tool.PendingPermissionCount++;
                effects.Add(SideEffect.SendToastNotification);
                effects.Add(SideEffect.AutoExpand);
                effects.Add(SideEffect.FlashCompactBar);
                break;

            case EventType.Stop:
                tool.ResponseCount++;
                if (tool.Status != ToolStatus.WaitingPermission && tool.Status != ToolStatus.Error)
                {
                    tool.Status = ToolStatus.Idle;
                }
                break;

            case EventType.Notification:
                // Update notification state but don't change tool status
                tool.LastActivity = hookEvent.DataJson;
                break;

            case EventType.UserPromptSubmit:
                tool.Status = ToolStatus.Running;
                break;

            case EventType.SubagentStart:
                tool.SubagentCount++;
                tool.Status = ToolStatus.Running;
                effects.Add(SideEffect.FlashCompactBar);
                break;

            case EventType.SubagentStop:
                // Subagent completion is informative
                break;

            case EventType.PreCompact:
            case EventType.PostCompact:
                // Compaction events are low-priority; just track timing
                break;

            case EventType.ResourceSample:
                // Handled separately by resource monitor
                break;

            case EventType.Status:
                // Bridge status messages
                break;
        }

        // Prune recent messages to last 3
        if (tool.RecentMessages.Count > 3)
        {
            tool.RecentMessages = tool.RecentMessages.TakeLast(3).ToList();
        }

        return (state, effects);
    }

    /// <summary>
    /// Apply a resource sample to the session state.
    /// </summary>
    public static SessionSnapshot ApplyResourceSample(
        SessionSnapshot currentState,
        ResourceSample sample)
    {
        var state = Clone(currentState);
        var tool = state.Tools.Find(t => t.ToolId == sample.Tool);
        if (tool != null)
        {
            tool.LatestResourceSample = sample;
        }
        state.LastUpdated = DateTimeOffset.UtcNow;
        return state;
    }

    /// <summary>
    /// Apply a token usage update to the session state.
    /// </summary>
    public static SessionSnapshot ApplyTokenUsage(
        SessionSnapshot currentState,
        string toolId,
        TokenUsage usage)
    {
        var state = Clone(currentState);
        var tool = state.Tools.Find(t => t.ToolId == toolId);
        if (tool != null)
        {
            tool.TokenUsage = usage;
        }
        state.LastUpdated = DateTimeOffset.UtcNow;
        return state;
    }

    /// <summary>
    /// Clear a permission request after it's been handled.
    /// </summary>
    public static SessionSnapshot ClearPermission(
        SessionSnapshot currentState,
        string toolId,
        bool wasApproved)
    {
        var state = Clone(currentState);
        var tool = state.Tools.Find(t => t.ToolId == toolId);
        if (tool != null && tool.PendingPermissionCount > 0)
        {
            tool.PendingPermissionCount--;
            if (tool.PendingPermissionCount == 0 && tool.Status == ToolStatus.WaitingPermission)
            {
                tool.Status = ToolStatus.Running;
            }
        }
        return state;
    }

    private static SessionSnapshot Clone(SessionSnapshot original)
    {
        return new SessionSnapshot
        {
            Tools = original.Tools.Select(t => new ToolState
            {
                ToolId = t.ToolId,
                Name = t.Name,
                Status = t.Status,
                CurrentToolCall = t.CurrentToolCall,
                LastActivity = t.LastActivity,
                ResponseCount = t.ResponseCount,
                ToolCallCount = t.ToolCallCount,
                SubagentCount = t.SubagentCount,
                PendingPermissionCount = t.PendingPermissionCount,
                RecentMessages = new List<string>(t.RecentMessages),
                ActiveSubagents = t.ActiveSubagents.Select(s => new SubagentInfo
                {
                    Name = s.Name,
                    Status = s.Status,
                    ElapsedSeconds = s.ElapsedSeconds
                }).ToList(),
                TokenUsage = t.TokenUsage,
                LatestResourceSample = t.LatestResourceSample,
                Terminal = t.Terminal,
                SessionStartTime = t.SessionStartTime,
                LastEventTime = t.LastEventTime
            }).ToList(),
            LastUpdated = original.LastUpdated
        };
    }

    private static string GetToolDisplayName(string toolId) => toolId switch
    {
        "claude" => "Claude Code",
        "codex" => "Codex CLI",
        "cursor" => "Cursor",
        "copilot" => "GitHub Copilot",
        "windsurf" => "Windsurf",
        "codebuddy" => "CodeBuddy",
        "gemini" => "Gemini CLI",
        _ => toolId
    };
}
