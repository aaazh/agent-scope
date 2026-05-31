//! AgentScope Bridge — Windows Named Pipe Server
//!
//! Collects hook events from AI coding CLI tools (Claude Code, Codex, etc.)
//! via injected hook configurations, normalizes them into a unified JSONL stream,
//! and delivers them to the AgentScope WPF app via Named Pipe.

mod adapters;
mod event;
mod monitor;
mod pipe;

use log::{error, info};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;

static RUNNING: AtomicBool = AtomicBool::new(true);

fn main() {
    env_logger::Builder::from_env(env_logger::Env::default().default_filter_or("info")).init();

    info!("AgentScope Bridge v{} starting...", env!("CARGO_PKG_VERSION"));

    // Set up Ctrl+C handler for graceful shutdown
    let running = Arc::new(AtomicBool::new(true));
    let r = running.clone();
    ctrlc::set_handler(move || {
        info!("Shutdown signal received, cleaning up...");
        r.store(false, Ordering::SeqCst);
    })
    .expect("Failed to set Ctrl+C handler");

    // Detect installed AI tools
    let detected = adapters::detect_all();
    info!("Detected {} AI CLI tool(s)", detected.len());
    for tool in &detected {
        info!("  - {} ({:?})", tool.name(), tool.status());
    }

    // Register hooks for detected tools
    for tool in &detected {
        if let Err(e) = tool.register_hooks() {
            error!("Failed to register hooks for {}: {}", tool.name(), e);
        } else {
            info!("Hooks registered for {}", tool.name());
        }
    }

    // Start Named Pipe server
    if let Err(e) = pipe::run_server(running.clone()) {
        error!("Named Pipe server error: {}", e);
    }

    // Cleanup: unregister hooks on exit
    info!("Cleaning up hook registrations...");
    for tool in &detected {
        if let Err(e) = tool.unregister_hooks() {
            error!("Failed to unregister hooks for {}: {}", tool.name(), e);
        }
    }

    info!("AgentScope Bridge shutdown complete.");
}
