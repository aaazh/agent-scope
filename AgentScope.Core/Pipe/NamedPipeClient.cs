using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AgentScope.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgentScope.Core.Pipe;

/// <summary>
/// Client for connecting to the agent-hooks-bridge Named Pipe server.
/// Handles JSONL streaming read and permission decision writes.
/// </summary>
public class NamedPipeClient : IDisposable
{
    private const string PipeName = "agentscope";
    private const int ReconnectDelayMs = 2000;
    private const int MaxReconnectDelayMs = 30000;

    private readonly ILogger? _logger;
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private int _reconnectAttempt;

    public event Action<HookEvent>? OnHookEvent;
    public event Action<string>? OnStatusChanged;

    public NamedPipeClient(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Start connecting and reading events asynchronously.</summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await Task.Run(() => ConnectLoop(_cts.Token), _cts.Token);
    }

    private async Task ConnectLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _pipe = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                _logger?.LogInformation("Connecting to Named Pipe: {PipeName}", PipeName);
                await _pipe.ConnectAsync(5000, ct);

                _reconnectAttempt = 0;
                _reader = new StreamReader(_pipe, Encoding.UTF8);
                _writer = new StreamWriter(_pipe, Encoding.UTF8) { AutoFlush = true };

                OnStatusChanged?.Invoke("connected");
                _logger?.LogInformation("Connected to bridge");

                await ReadLoop(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Pipe connection error, reconnecting...");
                OnStatusChanged?.Invoke("disconnected");
                _reconnectAttempt++;
                var delay = Math.Min(ReconnectDelayMs * _reconnectAttempt, MaxReconnectDelayMs);
                await Task.Delay(delay, ct);
            }
            finally
            {
                CleanupConnection();
            }
        }
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _reader != null)
        {
            var line = await _reader.ReadLineAsync(ct);
            if (line == null)
            {
                _logger?.LogInformation("Pipe server closed connection");
                OnStatusChanged?.Invoke("disconnected");
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var hookEvent = JsonSerializer.Deserialize<HookEvent>(line, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (hookEvent != null)
                {
                    OnHookEvent?.Invoke(hookEvent);
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize pipe message: {Line}", line);
            }
        }
    }

    /// <summary>Send a permission decision back to the bridge.</summary>
    public async Task SendPermissionDecision(string eventId, bool allow)
    {
        if (_writer == null) return;

        var msg = new
        {
            type = "permission_decision",
            event_id = eventId,
            decision = allow ? "allow" : "deny",
            version = "1.0"
        };
        var json = JsonSerializer.Serialize(msg);
        await _writer.WriteLineAsync(json);
        _logger?.LogInformation("Sent permission decision: {EventId} = {Decision}", eventId, msg.decision);
    }

    /// <summary>Request the bridge to refresh hook registrations.</summary>
    public async Task SendRefreshRequest()
    {
        if (_writer == null) return;

        var msg = new
        {
            type = "refresh_request",
            version = "1.0"
        };
        var json = JsonSerializer.Serialize(msg);
        await _writer.WriteLineAsync(json);
    }

    private void CleanupConnection()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _pipe?.Dispose();
        _reader = null;
        _writer = null;
        _pipe = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        CleanupConnection();
    }
}
