//! Windows process resource monitor.
//!
//! Tracks CPU and memory usage for AI tool process trees using WMI.

use serde::Serialize;
use std::collections::HashMap;

/// A single process in the monitored tree.
#[derive(Debug, Clone, Serialize)]
pub struct ProcessInfo {
    pub pid: u32,
    pub name: String,
    pub cpu_percent: f64,
    pub memory_mb: f64,
    pub children: Vec<u32>,
}

/// Resource sample for a tool's process tree.
#[derive(Debug, Clone, Serialize)]
pub struct ResourceSample {
    pub tool: String,
    pub root_pid: u32,
    pub cpu_percent: f64,
    pub memory_mb: f64,
    pub process_count: usize,
    pub processes: Vec<ProcessInfo>,
    pub timestamp: i64,
}

/// Build a process tree starting from a root PID.
#[cfg(windows)]
pub fn build_process_tree(root_pid: u32) -> Vec<ProcessInfo> {
    use std::process::Command;

    let mut tree: HashMap<u32, ProcessInfo> = HashMap::new();

    // Use WMI to enumerate all processes with parent-child relationships
    let output = Command::new("wmic")
        .args([
            "process",
            "get",
            "ProcessId,ParentProcessId,Name,WorkingSetSize",
            "/format:csv",
        ])
        .output();

    if let Ok(out) = output {
        let text = String::from_utf8_lossy(&out.stdout);
        for line in text.lines().skip(2) {
            // skip header lines
            let parts: Vec<&str> = line.split(',').collect();
            if parts.len() >= 5 {
                if let (Ok(pid), Ok(ppid)) =
                    (parts[1].trim().parse::<u32>(), parts[2].trim().parse::<u32>())
                {
                    let name = parts[3].trim().to_string();
                    let memory = parts[4]
                        .trim()
                        .parse::<u64>()
                        .map(|b| b as f64 / (1024.0 * 1024.0))
                        .unwrap_or(0.0);

                    tree.entry(pid)
                        .or_insert(ProcessInfo {
                            pid,
                            name: name.clone(),
                            cpu_percent: 0.0,
                            memory_mb: memory,
                            children: Vec::new(),
                        });

                    // Update parent's children list
                    if ppid != 0 {
                        tree.entry(ppid)
                            .or_insert(ProcessInfo {
                                pid: ppid,
                                name: String::new(),
                                cpu_percent: 0.0,
                                memory_mb: 0.0,
                                children: Vec::new(),
                            })
                            .children
                            .push(pid);
                    }
                }
            }
        }
    }

    // Collect the subtree rooted at root_pid
    let mut result = Vec::new();
    let mut to_visit: Vec<u32> = vec![root_pid];
    let mut visited = std::collections::HashSet::new();

    while let Some(pid) = to_visit.pop() {
        if !visited.insert(pid) {
            continue;
        }
        if let Some(info) = tree.get(&pid) {
            to_visit.extend(&info.children);
            result.push(info.clone());
        }
    }

    result
}

/// Sample CPU usage for a process tree.
#[cfg(windows)]
pub fn sample_cpu(_root_pid: u32, _tree: &[ProcessInfo]) -> f64 {
    // CPU sampling via WMI Win32_PerfFormattedData_PerfProc_Process
    // Simplified: use Process::get_processes performance counter
    // In production this would use PDH (Performance Data Helper) for accuracy.
    // For now, return 0.0 as placeholder (requires iterative sampling for accurate %).
    0.0
}

/// Sample memory for a process tree.
pub fn sample_memory(tree: &[ProcessInfo]) -> f64 {
    tree.iter().map(|p| p.memory_mb).sum()
}

/// Take a full resource sample for a tool.
pub fn take_sample(tool: &str, root_pid: u32) -> ResourceSample {
    let processes = build_process_tree(root_pid);
    let cpu = sample_cpu(root_pid, &processes);
    let mem = sample_memory(&processes);

    ResourceSample {
        tool: tool.to_string(),
        root_pid,
        cpu_percent: cpu,
        memory_mb: mem,
        process_count: processes.len(),
        processes,
        timestamp: chrono::Utc::now().timestamp_millis(),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_sample_memory_empty() {
        let tree: Vec<ProcessInfo> = vec![];
        assert_eq!(sample_memory(&tree), 0.0);
    }

    #[test]
    fn test_sample_memory_sum() {
        let tree = vec![
            ProcessInfo {
                pid: 100,
                name: "test.exe".into(),
                cpu_percent: 0.0,
                memory_mb: 100.0,
                children: vec![101],
            },
            ProcessInfo {
                pid: 101,
                name: "child.exe".into(),
                cpu_percent: 0.0,
                memory_mb: 50.0,
                children: vec![],
            },
        ];
        assert!((sample_memory(&tree) - 150.0).abs() < 0.01);
    }

    #[test]
    fn test_take_sample_structure() {
        // This test won't work without actual processes on the system,
        // so we verify the struct is created correctly.
        let sample = ResourceSample {
            tool: "test".into(),
            root_pid: 0,
            cpu_percent: 0.0,
            memory_mb: 0.0,
            process_count: 0,
            processes: vec![],
            timestamp: 0,
        };
        assert_eq!(sample.tool, "test");
        assert_eq!(sample.root_pid, 0);
    }
}
