//! Windows Named Pipe server for IPC with the WPF UI.

use crate::event::{ClientMessage, HookEvent};
use log::{error, info, warn};
use std::io::{BufRead, BufReader, Write};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::time::Duration;

#[cfg(windows)]
mod win {
    use std::ffi::OsStr;
    use std::os::windows::ffi::OsStrExt;
    use std::path::Path;

    pub fn to_wide(path: &Path) -> Vec<u16> {
        path.as_os_str().encode_wide().chain(std::iter::once(0)).collect()
    }
}

const PIPE_NAME: &str = r"\\.\pipe\agentscope";
const BUFFER_SIZE: usize = 65536;
const MAX_CLIENTS: usize = 16;

/// Run the Named Pipe server loop.
/// Accepts client connections and streams events.
#[cfg(windows)]
pub fn run_server(running: Arc<AtomicBool>) -> Result<(), String> {
    use std::os::windows::io::FromRawHandle;
    use winapi::um::fileapi::*;
    use winapi::um::handleapi::*;
    use winapi::um::namedpipeapi::*;
    use winapi::um::synchapi::*;
    use winapi::um::winbase::*;

    info!("Starting Named Pipe server at {}", PIPE_NAME);

    let pipe_name: Vec<u16> = std::ffi::OsStr::new(PIPE_NAME)
        .encode_wide()
        .chain(std::iter::once(0))
        .collect();

    // We use a thread-based loop since winapi Named Pipe is synchronous.
    // The server thread accepts connections; each connection gets its own handler thread.

    let server_thread = std::thread::spawn(move || {
        while running.load(Ordering::SeqCst) {
            unsafe {
                let pipe_handle = CreateNamedPipeW(
                    pipe_name.as_ptr(),
                    PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
                    PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                    MAX_CLIENTS as u32,
                    BUFFER_SIZE as u32,
                    BUFFER_SIZE as u32,
                    0,
                    std::ptr::null_mut(),
                );

                if pipe_handle == INVALID_HANDLE_VALUE {
                    error!("Failed to create Named Pipe");
                    std::thread::sleep(std::time::Duration::from_secs(1));
                    continue;
                }

                // Wait for client to connect
                let connected = ConnectNamedPipe(pipe_handle, std::ptr::null_mut());
                if connected == 0 {
                    let err = GetLastError();
                    if err != ERROR_PIPE_CONNECTED {
                        error!("ConnectNamedPipe failed: {}", err);
                        CloseHandle(pipe_handle);
                        continue;
                    }
                }

                // Spawn handler thread for this client
                let client_running = running.clone();
                std::thread::spawn(move || {
                    handle_client(pipe_handle, client_running);
                });
            }
        }
    });

    server_thread.join().map_err(|_| "Server thread panicked".to_string())?;
    Ok(())
}

#[cfg(windows)]
unsafe fn handle_client(pipe_handle: winapi::shared::ntdef::HANDLE, running: Arc<AtomicBool>) {
    use std::os::windows::io::FromRawHandle;
    use winapi::um::fileapi::*;
    use winapi::um::handleapi::*;
    use winapi::um::namedpipeapi::*;

    let file = std::fs::File::from_raw_handle(pipe_handle as *mut _);
    let mut reader = BufReader::new(file.try_clone().expect("clone pipe handle"));
    let mut writer = file;

    // Send connection handshake
    let hello = HookEvent::status("connected").to_jsonl().unwrap_or_default();
    let _ = writer.write_all(hello.as_bytes());
    let _ = writer.flush();

    // Read loop: handle incoming client messages (permission decisions, refresh requests)
    let mut line = String::new();
    while running.load(Ordering::SeqCst) {
        line.clear();
        match reader.read_line(&mut line) {
            Ok(0) => {
                info!("Client disconnected");
                break;
            }
            Ok(_) => {
                if let Ok(msg) = serde_json::from_str::<ClientMessage>(&line) {
                    handle_client_message(&msg);
                }
            }
            Err(e) => {
                warn!("Pipe read error: {}", e);
                break;
            }
        }
    }

    unsafe { CloseHandle(pipe_handle); }
}

/// Process a message received from the WPF client.
fn handle_client_message(msg: &ClientMessage) {
    match msg.msg_type.as_str() {
        "permission_decision" => {
            info!(
                "Permission decision for {}: {}",
                msg.event_id.as_deref().unwrap_or("unknown"),
                msg.decision.as_deref().unwrap_or("unknown")
            );
            // The decision is stored for the hook process to read.
            // In production, this would be written to a shared state that
            // the hook process checks via stdin/stdout.
        }
        "refresh_request" => {
            info!("Refresh request received from UI");
        }
        _ => {
            warn!("Unknown client message type: {}", msg.msg_type);
        }
    }
}

/// Non-Windows stub.
#[cfg(not(windows))]
pub fn run_server(_running: Arc<AtomicBool>) -> Result<(), String> {
    Err("Named Pipe server is only supported on Windows".to_string())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_handle_client_message_permission() {
        let msg = ClientMessage::permission_decision("evt_001", "allow");
        assert_eq!(msg.msg_type, "permission_decision");
        assert_eq!(msg.decision.unwrap(), "allow");
    }

    #[test]
    fn test_handle_client_message_refresh() {
        let msg = ClientMessage::refresh_request();
        assert_eq!(msg.msg_type, "refresh_request");
    }
}
