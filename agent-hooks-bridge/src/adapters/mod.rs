//! Tool adapter trait and detection logic.
//!
//! Each supported AI CLI tool implements the `ToolAdapter` trait,
//! enabling unified hook registration and event collection.

use std::path::PathBuf;

pub mod claude;
pub mod codex;

/// Status of a tool as detected by the bridge.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ToolStatus {
    Detected,
    NotDetected,
    HookRegistered,
    HookError(String),
}

/// Platform-specific configuration for a tool adapter.
#[derive(Debug, Clone)]
pub struct PlatformConfig {
    /// True when running on Windows; affects config paths.
    pub is_windows: bool,
}

impl Default for PlatformConfig {
    fn default() -> Self {
        Self {
            is_windows: cfg!(windows),
        }
    }
}

/// Unified trait for tool hook adapters.
pub trait ToolAdapter {
    /// Human-readable name of the tool (e.g., "Claude Code").
    fn name(&self) -> &str;

    /// Machine-friendly tool id (e.g., "claude").
    fn tool_id(&self) -> &str;

    /// Current detection/registration status.
    fn status(&self) -> ToolStatus;

    /// Detect whether the tool CLI is available on the system PATH.
    fn detect(&mut self) -> ToolStatus;

    /// Return paths to the tool's configuration file(s).
    fn config_paths(&self) -> Vec<PathBuf>;

    /// Register AgentScope hooks into the tool's configuration.
    fn register_hooks(&self) -> Result<(), String>;

    /// Remove AgentScope hooks from the tool's configuration.
    fn unregister_hooks(&self) -> Result<(), String>;

    /// Return the platform configuration for this adapter.
    fn platform_config(&self) -> PlatformConfig {
        PlatformConfig::default()
    }

    /// Return the bridge command that hooks should invoke.
    fn bridge_command(&self) -> Vec<String> {
        vec![std::env::current_exe()
            .unwrap_or_else(|_| PathBuf::from("agent-hooks-bridge.exe"))
            .to_string_lossy()
            .to_string()]
    }
}

/// Check if a command exists on the system PATH.
#[cfg(windows)]
pub fn command_exists(cmd: &str) -> bool {
    std::process::Command::new("where")
        .arg(cmd)
        .stdout(std::process::Stdio::null())
        .stderr(std::process::Stdio::null())
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

#[cfg(not(windows))]
pub fn command_exists(cmd: &str) -> bool {
    std::process::Command::new("which")
        .arg(cmd)
        .stdout(std::process::Stdio::null())
        .stderr(std::process::Stdio::null())
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

/// Detect all installed AI CLI tools on the system.
pub fn detect_all() -> Vec<Box<dyn ToolAdapter>> {
    let mut tools: Vec<Box<dyn ToolAdapter>> = vec![
        Box::new(claude::ClaudeAdapter::new()),
        Box::new(codex::CodexAdapter::new()),
    ];

    for tool in &mut tools {
        tool.detect();
    }

    tools
}
