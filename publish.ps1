# AgentScope Release Build Script
# Usage: .\publish.ps1 [-Version "0.1.0-alpha"]
param([string]$Version = "0.1.0-alpha")

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Building AgentScope v$Version ==="

# Rust Bridge
Write-Host "[1/4] Building agent-hooks-bridge (Rust)..."
Set-Location "$Root\agent-hooks-bridge"
$env:RUSTFLAGS = "-C linker=rust-lld"
$env:LIB = "C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64;C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\ucrt\x64"
cargo build --release
if ($LASTEXITCODE -ne 0) { throw "Rust build failed" }

# .NET Publish
Write-Host "[2/4] Publishing AgentScope.App (C#)..."
Set-Location "$Root"
dotnet publish AgentScope.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Copy artifacts
$PublishDir = "$Root\publish\AgentScope"
$AppPublishDir = "$Root\AgentScope.App\bin\Release\net8.0-windows\win-x64\publish"
Write-Host "[3/4] Copying files to $PublishDir..."
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
Copy-Item "$AppPublishDir\*" -Destination $PublishDir -Recurse -Force
Copy-Item "$Root\agent-hooks-bridge\target\release\agent-hooks-bridge.exe" -Destination $PublishDir -Force

# Copy assets
Copy-Item "$Root\AgentScope.App\Assets" -Destination "$PublishDir\Assets" -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$Root\AgentScope.App\Locales" -Destination "$PublishDir\Locales" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "[4/4] Build complete!"
Write-Host "  Output: $PublishDir"
Write-Host "  AgentScope.exe: $((Get-Item "$PublishDir\AgentScope.exe").Length / 1MB) MB"
Write-Host "  agent-hooks-bridge.exe: $((Get-Item "$PublishDir\agent-hooks-bridge.exe").Length / 1KB) KB"
