using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class TerminalSession : IDisposable
{
    private readonly ShellProfile _profile;
    private readonly ConPtySession _session;
    private bool _disposed;

    private TerminalSession(ShellProfile profile, ConPtySession session)
    {
        _profile = profile;
        _session = session;
        _session.OutputReceived += (_, text) => OutputReceived?.Invoke(this, new TerminalOutputEventArgs(text));
        _session.Exited += (_, exitCode) =>
        {
            ExitCode = exitCode;
            EndedAt = DateTimeOffset.Now;
            Exited?.Invoke(this, new TerminalSessionExitedEventArgs(exitCode));
        };
    }

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;

    public event EventHandler<TerminalSessionExitedEventArgs>? Exited;

    public ShellProfile Profile => _profile;

    public int ProcessId => _session.ProcessId;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

    public DateTimeOffset? EndedAt { get; private set; }

    public int? ExitCode { get; private set; }

    public bool IsRunning => ExitCode is null;

    public static TerminalSession Start(ShellProfile profile, short columns = 140, short rows = 40)
    {
        var session = ConPtySession.Start(
            profile.ExecutablePath,
            profile.Arguments,
            profile.StartingDirectory,
            profile.EnvironmentOverrides,
            columns,
            rows);

        return new TerminalSession(profile, session);
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default) =>
        _session.SendInputAsync(text, cancellationToken);

    public void Resize(short columns, short rows) => _session.Resize(columns, rows);

    public void Terminate()
    {
        _session.Terminate();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Dispose();
    }
}
