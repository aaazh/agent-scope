//! Unified event model for normalized hook events across all AI tools.

use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

/// Unified event types, normalized across all supported AI tools.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "PascalCase")]
pub enum EventType {
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
    /// Custom event type for resource monitoring samples
    ResourceSample,
    /// Status/heartbeat message from bridge
    Status,
}

/// A normalized hook event from any AI tool.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HookEvent {
    /// Unique event identifier (UUID v4)
    pub event_id: String,
    /// Message type discriminator
    #[serde(rename = "type")]
    pub msg_type: String,
    /// Normalized event type
    pub event: EventType,
    /// Source tool identifier (e.g., "claude", "codex")
    pub tool: String,
    /// Tool-specific event data payload
    pub data: serde_json::Value,
    /// Unix timestamp in milliseconds
    pub timestamp: i64,
    /// Protocol version for forward compatibility
    #[serde(default = "default_version")]
    pub version: String,
}

fn default_version() -> String {
    "1.0".to_string()
}

impl HookEvent {
    /// Create a new hook event.
    pub fn new(event: EventType, tool: &str, data: serde_json::Value) -> Self {
        Self {
            event_id: Uuid::new_v4().to_string(),
            msg_type: "hook_event".to_string(),
            event,
            tool: tool.to_string(),
            data,
            timestamp: Utc::now().timestamp_millis(),
            version: "1.0".to_string(),
        }
    }

    /// Create a resource sample event.
    pub fn resource_sample(tool: &str, data: serde_json::Value) -> Self {
        Self {
            event_id: Uuid::new_v4().to_string(),
            msg_type: "resource_sample".to_string(),
            event: EventType::ResourceSample,
            tool: tool.to_string(),
            data,
            timestamp: Utc::now().timestamp_millis(),
            version: "1.0".to_string(),
        }
    }

    /// Create a status message.
    pub fn status(status: &str) -> Self {
        Self {
            event_id: Uuid::new_v4().to_string(),
            msg_type: "status".to_string(),
            event: EventType::Status,
            tool: "agentscope".to_string(),
            data: serde_json::json!({"status": status}),
            timestamp: Utc::now().timestamp_millis(),
            version: "1.0".to_string(),
        }
    }

    /// Serialize to a JSONL line (one-line JSON + newline).
    pub fn to_jsonl(&self) -> Result<String, serde_json::Error> {
        let mut json = serde_json::to_string(self)?;
        json.push('\n');
        Ok(json)
    }
}

/// Message sent from the WPF UI back to the bridge.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ClientMessage {
    #[serde(rename = "type")]
    pub msg_type: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub event_id: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub decision: Option<String>,
    #[serde(default = "default_version")]
    pub version: String,
}

impl ClientMessage {
    /// Create a permission decision message.
    pub fn permission_decision(event_id: &str, decision: &str) -> Self {
        Self {
            msg_type: "permission_decision".to_string(),
            event_id: Some(event_id.to_string()),
            decision: Some(decision.to_string()),
            version: "1.0".to_string(),
        }
    }

    /// Create a refresh request message.
    pub fn refresh_request() -> Self {
        Self {
            msg_type: "refresh_request".to_string(),
            event_id: None,
            decision: None,
            version: "1.0".to_string(),
        }
    }
}
