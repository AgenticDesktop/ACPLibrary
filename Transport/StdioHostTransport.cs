using System.Text;

namespace Agentic.ACPLibrary.Transport;

/// <summary>
/// Agent-side stdin/stdout transport.
/// The Agent process IS the subprocess — it communicates via its own stdin/stdout.
/// </summary>
public sealed class StdioHostTransport : IAgentTransport
{
    private TransportState _state = TransportState.Created;
    private CancellationTokenSource? _readCts;
    private Stream? _stdin;
    private Stream? _stdout;
    private StreamWriter? _writer;

    public TransportState State => _state;

    public event Func<string, Task>? MessageReceived;
    public event Func<Exception, Task>? TransportFaulted;
    public event Func<int, Task>? ProcessExited;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _state = TransportState.Starting;

        _stdin = Console.OpenStandardInput();
        _stdout = Console.OpenStandardOutput();
        // BOM-less UTF-8: a BOM before the first JSON-RPC line would break strict clients
        _writer = new StreamWriter(_stdout, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };

        _readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => ReadLoopAsync(_stdin, _readCts.Token), _readCts.Token);

        _state = TransportState.Running;
        return Task.CompletedTask;
    }

    public async Task SendAsync(string jsonLine, CancellationToken cancellationToken = default)
    {
        if (_writer is null || _state != TransportState.Running)
            throw new InvalidOperationException("Transport is not running.");

        await _writer.WriteLineAsync(jsonLine.AsMemory(), cancellationToken);
        await _writer.FlushAsync(cancellationToken);
    }

    public Task StopAsync()
    {
        if (_state == TransportState.Stopped || _state == TransportState.Stopping)
            return Task.CompletedTask;

        _state = TransportState.Stopping;
        _readCts?.Cancel();

        _state = TransportState.Stopped;
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(Stream stdin, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(stdin, Encoding.UTF8);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break; // EOF — Client disconnected
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (MessageReceived is not null)
                    await MessageReceived(line);
            }

            // stdin closed (EOF) — Client disconnected
            if (ProcessExited is not null)
                await ProcessExited(0);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            if (TransportFaulted is not null)
                await TransportFaulted(ex);
        }
    }
}
