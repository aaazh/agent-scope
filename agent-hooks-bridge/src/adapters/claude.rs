//! Claude Code tool adapter.
//!
//! Handles hook registration for Claude Code by reading/writing
//! `%USERPROFILE%\.claude\settings.json`.

use super::{command_exists, PlatformConfig, ToolAdapter, ToolStatus};
use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

const TOOL_NAME: &str = "Claude Code";
const TOOL_ID: &str = "claude";

/// Subset of Claude Code settings.json relevant to hooks.
#[derive(Debug, Serialize, Deserialize, Clone)]
struct ClaudeSettings {
    #[serde(skip_serializing_if = "Option::is_none")]
    hooks: Option<ClaudeHooks>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct ClaudeHooks {
    #[serde(skip_serializing_if = "Option::is_none")]
    PreToolUse: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    PostToolUse: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    PostToolUseFailure: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    PermissionRequest: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    Stop: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    Notification: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    UserPromptSubmit: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    SubagentStart: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    SubagentStop: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    SessionStart: Option<Vec<HookEntry>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    SessionEnd: Option<Vec<HookEntry>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct HookEntry {
    matcher: Option<String>,
    hooks: Vec<HookCommand>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct HookCommand {
    #[serde(rename = "type")]
    hook_type: String,
    command: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    timeout: Option<u64>,
}

const AGENTSCOPE_COMMAND: &str = "agent-hooks-bridge";

pub struct ClaudeAdapter {
    status: ToolStatus,
    config_path: PathBuf,
}

impl ClaudeAdapter {
    pub fn new() -> Self {
        let config_path = Self::default_config_path();
        Self {
            status: ToolStatus::NotDetected,
            config_path,
        }
    }

    fn default_config_path() -> PathBuf {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("."))
            .join(".claude")
            .join("settings.json")
    }

    /// Create list of hook events we want to register for Claude Code.
    fn hook_events() -> Vec<&'static str> {
        vec![
            "PreToolUse",
            "PostToolUse",
            "PostToolUseFailure",
            "PermissionRequest",
            "Stop",
            "Notification",
            "UserPromptSubmit",
            "SubagentStart",
            "SubagentStop",
            "SessionStart",
            "SessionEnd",
        ]
    }

    /// Build the hook command entry that routes events to the bridge.
    fn build_hook_entry(&self) -> HookEntry {
        HookEntry {
            matcher: Some("".to_string()), // match all
            hooks: vec![HookCommand {
                hook_type: "command".to_string(),
                command: self.bridge_command().join(" "),
                timeout: Some(30000),
            }],
        }
    }

    /// Check if a hook entry is already an AgentScope hook.
    fn is_agentscope_hook(entry: &HookEntry) -> bool {
        entry
            .hooks
            .iter()
            .any(|h| h.command.contains(AGENTSCOPE_COMMAND))
    }
}

impl ToolAdapter for ClaudeAdapter {
    fn name(&self) -> &str {
        TOOL_NAME
    }

    fn tool_id(&self) -> &str {
        TOOL_ID
    }

    fn status(&self) -> ToolStatus {
        self.status.clone()
    }

    fn detect(&mut self) -> ToolStatus {
        if command_exists("claude") {
            self.status = ToolStatus::Detected;
        } else {
            self.status = ToolStatus::NotDetected;
        }
        self.status.clone()
    }

    fn config_paths(&self) -> Vec<PathBuf> {
        vec![self.config_path.clone()]
    }

    fn register_hooks(&self) -> Result<(), String> {
        // Read existing config
        let mut settings: ClaudeSettings = if self.config_path.exists() {
            let content = fs::read_to_string(&self.config_path).map_err(|e| {
                format!("Failed to read {}: {}", self.config_path.display(), e)
            })?;
            serde_json::from_str(&content).unwrap_or(ClaudeSettings { hooks: None })
        } else {
            ClaudeSettings { hooks: None }
        };

        let hook_entry = self.build_hook_entry();
        let mut hooks = settings.hooks.unwrap_or(ClaudeHooks {
            PreToolUse: None,
            PostToolUse: None,
            PostToolUseFailure: None,
            PermissionRequest: None,
            Stop: None,
            Notification: None,
            UserPromptSubmit: None,
            SubagentStart: None,
            SubagentStop: None,
            SessionStart: None,
            SessionEnd: None,
        });

        // Add hook entry to each event if not already present
        macro_rules! register_event {
            ($field:ident) => {
                let entries = hooks.$field.get_or_insert_with(Vec::new);
                if !entries.iter().any(Self::is_agentscope_hook) {
                    entries.push(hook_entry.clone());
                }
            };
        }

        register_event!(PreToolUse);
        register_event!(PostToolUse);
        register_event!(PostToolUseFailure);
        register_event!(PermissionRequest);
        register_event!(Stop);
        register_event!(Notification);
        register_event!(UserPromptSubmit);
        register_event!(SubagentStart);
        register_event!(SubagentStop);
        register_event!(SessionStart);
        register_event!(SessionEnd);

        let updated = serde_json::to_string_pretty(&ClaudeSettings {
            hooks: Some(hooks),
        })
        .map_err(|e| format!("Failed to serialize settings: {}", e))?;

        // Ensure parent directory exists
        if let Some(parent) = self.config_path.parent() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create config dir: {}", e))?;
        }

        fs::write(&self.config_path, updated)
            .map_err(|e| format!("Failed to write settings: {}", e))?;

        log::info!("Claude Code hooks registered at {}", self.config_path.display());
        Ok(())
    }

    fn unregister_hooks(&self) -> Result<(), String> {
        if !self.config_path.exists() {
            return Ok(());
        }

        let content = fs::read_to_string(&self.config_path).map_err(|e| {
            format!("Failed to read {}: {}", self.config_path.display(), e)
        })?;

        let mut settings: ClaudeSettings =
            serde_json::from_str(&content).unwrap_or(ClaudeSettings { hooks: None });

        if let Some(ref mut hooks) = settings.hooks {
            macro_rules! unregister_event {
                ($field:ident) => {
                    if let Some(ref mut entries) = hooks.$field {
                        entries.retain(|e| !Self::is_agentscope_hook(e));
                        if entries.is_empty() {
                            hooks.$field = None;
                        }
                    }
                };
            }

            unregister_event!(PreToolUse);
            unregister_event!(PostToolUse);
            unregister_event!(PostToolUseFailure);
            unregister_event!(PermissionRequest);
            unregister_event!(Stop);
            unregister_event!(Notification);
            unregister_event!(UserPromptSubmit);
            unregister_event!(SubagentStart);
            unregister_event!(SubagentStop);
            unregister_event!(SessionStart);
            unregister_event!(SessionEnd);
        }

        let updated = serde_json::to_string_pretty(&settings)
            .map_err(|e| format!("Failed to serialize settings: {}", e))?;

        fs::write(&self.config_path, updated)
            .map_err(|e| format!("Failed to write settings: {}", e))?;

        log::info!("Claude Code hooks unregistered from {}", self.config_path.display());
        Ok(())
    }

    fn platform_config(&self) -> PlatformConfig {
        PlatformConfig {
            is_windows: cfg!(windows),
        }
    }
}
