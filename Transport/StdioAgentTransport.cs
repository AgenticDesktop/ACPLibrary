using System.Diagnostics;

namespace Agentic.ACPLibrary.Transport;

/// <summary>
/// Transport implementation based on child process stdio.
/// </summary>
public sealed class StdioAgentTransport : IAgentTransport
{
    private readonly string _command;
    private readonly string _arguments;
    private readonly string? _workingDirectory;
    private Process? _process;
    private TransportState _state = TransportState.Created;
    private CancellationTokenSource? _readCts;

    public TransportState State => _state;

    public event Func<string, Task>? MessageReceived;
    public event Func<Exception, Task>? TransportFaulted;
    public event Func<int, Task>? ProcessExited;

    public StdioAgentTransport(string command, string arguments, string? workingDirectory = null)
    {
        _command = command;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _state = TransportState.Starting;

        var startInfo = new ProcessStartInfo
        {
            FileName = _command,
            Arguments = _arguments,
            WorkingDirectory = _workingDirectory ?? Directory.GetCurrentDirectory(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // BOM-less UTF-8 for stdin: default is the OS ANSI codepage (garbles non-ASCII prompts),
            // and Encoding.UTF8 would emit a BOM that corrupts the agent's first JSON-RPC line
            StandardInputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnProcessExited;

        _process.Start();

        _readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => ReadLoopAsync(_process.StandardOutput, _readCts.Token), _readCts.Token);
        _ = Task.Run(() => ReadStderrAsync(_process.StandardError, _readCts.Token), _readCts.Token);

        _state = TransportState.Running;
        return Task.CompletedTask;
    }

    public async Task SendAsync(string jsonLine, CancellationToken cancellationToken = default)
    {
        if (_process is null || _state != TransportState.Running)
            throw new InvalidOperationException("Transport is not running.");

        await _process.StandardInput.WriteLineAsync(jsonLine.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }

    public async Task StopAsync()
    {
        if (_state == TransportState.Stopped || _state == TransportState.Stopping)
            return;

        _state = TransportState.Stopping;
        _readCts?.Cancel();

        if (_process is not null && !_process.HasExited)
        {
            try
            {
                _process.StandardInput.Close();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _process.Kill(entireProcessTree: true);
            }
        }

        _state = TransportState.Stopped;
    }

    private async Task ReadLoopAsync(System.IO.StreamReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break; // EOF
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (MessageReceived is not null)
                    await MessageReceived(line);
            }
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

    private async Task ReadStderrAsync(System.IO.StreamReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;
                // stderr used for diagnostics, not handled for now
            }
        }
        catch
        {
            // stderr read failure can be ignored
        }
    }

    private async void OnProcessExited(object? sender, EventArgs e)
    {
        if (_process is null) return;
        var exitCode = _process.ExitCode;
        _state = TransportState.Stopped;

        if (ProcessExited is not null)
            await ProcessExited(exitCode);
    }
}
