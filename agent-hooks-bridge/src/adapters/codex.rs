//! Codex CLI tool adapter.
//!
//! Handles hook registration for OpenAI Codex CLI by reading/writing
//! `%USERPROFILE%\.codex\hooks.json` with `config.toml` notify fallback.

use super::{command_exists, PlatformConfig, ToolAdapter, ToolStatus};
use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

const TOOL_NAME: &str = "Codex CLI";
const TOOL_ID: &str = "codex";

/// Codex hooks.json structure.
#[derive(Debug, Serialize, Deserialize, Clone)]
struct CodexHooksConfig {
    #[serde(skip_serializing_if = "Option::is_none")]
    hooks: Option<Vec<CodexHookEntry>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct CodexHookEntry {
    event: String,
    command: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    timeout_ms: Option<u64>,
}

const AGENTSCOPE_COMMAND: &str = "agent-hooks-bridge";

pub struct CodexAdapter {
    status: ToolStatus,
    hooks_path: PathBuf,
}

impl CodexAdapter {
    pub fn new() -> Self {
        let hooks_path = Self::default_hooks_path();
        Self {
            status: ToolStatus::NotDetected,
            hooks_path,
        }
    }

    fn default_hooks_path() -> PathBuf {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("."))
            .join(".codex")
            .join("hooks.json")
    }

    fn hook_events() -> Vec<&'static str> {
        vec!["postToolUse", "userPromptSubmitted", "errorOccurred"]
    }
}

impl ToolAdapter for CodexAdapter {
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
        if command_exists("codex") {
            self.status = ToolStatus::Detected;
        } else {
            self.status = ToolStatus::NotDetected;
        }
        self.status.clone()
    }

    fn config_paths(&self) -> Vec<PathBuf> {
        vec![self.hooks_path.clone()]
    }

    fn register_hooks(&self) -> Result<(), String> {
        let mut config: CodexHooksConfig = if self.hooks_path.exists() {
            let content = fs::read_to_string(&self.hooks_path).map_err(|e| {
                format!("Failed to read {}: {}", self.hooks_path.display(), e)
            })?;
            serde_json::from_str(&content).unwrap_or(CodexHooksConfig { hooks: None })
        } else {
            CodexHooksConfig { hooks: None }
        };

        let mut hooks = config.hooks.unwrap_or_default();
        let bridge_cmd = self.bridge_command().join(" ");

        for event_name in Self::hook_events() {
            let already_registered = hooks.iter().any(|h| {
                h.event == event_name && h.command.contains(AGENTSCOPE_COMMAND)
            });

            if !already_registered {
                hooks.push(CodexHookEntry {
                    event: event_name.to_string(),
                    command: bridge_cmd.clone(),
                    timeout_ms: Some(30000),
                });
            }
        }

        let updated = serde_json::to_string_pretty(&CodexHooksConfig {
            hooks: Some(hooks),
        })
        .map_err(|e| format!("Failed to serialize hooks config: {}", e))?;

        if let Some(parent) = self.hooks_path.parent() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create config dir: {}", e))?;
        }

        fs::write(&self.hooks_path, updated)
            .map_err(|e| format!("Failed to write hooks config: {}", e))?;

        log::info!("Codex hooks registered at {}", self.hooks_path.display());
        Ok(())
    }

    fn unregister_hooks(&self) -> Result<(), String> {
        if !self.hooks_path.exists() {
            return Ok(());
        }

        let content = fs::read_to_string(&self.hooks_path).map_err(|e| {
            format!("Failed to read {}: {}", self.hooks_path.display(), e)
        })?;

        let mut config: CodexHooksConfig =
            serde_json::from_str(&content).unwrap_or(CodexHooksConfig { hooks: None });

        if let Some(ref mut hooks) = config.hooks {
            hooks.retain(|h| !h.command.contains(AGENTSCOPE_COMMAND));
            if hooks.is_empty() {
                config.hooks = None;
            }
        }

        let updated = serde_json::to_string_pretty(&config)
            .map_err(|e| format!("Failed to serialize hooks config: {}", e))?;

        fs::write(&self.hooks_path, updated)
            .map_err(|e| format!("Failed to write hooks config: {}", e))?;

        log::info!("Codex hooks unregistered from {}", self.hooks_path.display());
        Ok(())
    }

    fn platform_config(&self) -> PlatformConfig {
        PlatformConfig {
            is_windows: cfg!(windows),
        }
    }
}
