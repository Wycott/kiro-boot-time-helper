using UptimeTracker.BootTime;
using UptimeTracker.Console;
using UptimeTracker.Models;

namespace UptimeTracker;

internal sealed class UptimeRenderer
{
    private readonly IBootTimeProvider _bootTimeProvider;
    private readonly ThresholdConfiguration _config;
    private readonly IConsoleWriter _console;
    private readonly bool _testMode;
    private readonly Func<int, CancellationToken, Task>? _delayFunc;

    public UptimeRenderer(
        IBootTimeProvider bootTimeProvider,
        ThresholdConfiguration config,
        IConsoleWriter console,
        bool testMode = false,
        Func<int, CancellationToken, Task>? delayFunc = null)
    {
        _bootTimeProvider = bootTimeProvider;
        _config = config;
        _console = console;
        _testMode = testMode;
        _delayFunc = delayFunc;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Record the cursor row where uptime will be written in-place
        int uptimeRow = _console.CursorTop;

        int flashTick = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bootTime = _bootTimeProvider.GetBootTime();
                var uptime = DateTime.Now - bootTime;
                var colorState = ThresholdResolver.Resolve(uptime, _config, flashTick);

                // Move cursor to the uptime line (in-place update)
                _console.SetCursorPosition(0, uptimeRow);

                // Apply colors and write uptime
                ApplyColors(colorState);
                _console.Write(UptimeFormatter.Format(uptime));
                _console.ResetColor();

                flashTick++;

                // Determine delay based on state
                int delayMs = colorState is ColorState.Overdue
                    ? _config.Overdue.FlashIntervalMs
                    : 1000;

                try
                {
                    if (_delayFunc != null)
                        await _delayFunc(delayMs, cancellationToken);
                    else
                        await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            // Cleanup: restore colors and print final newline
            _console.ResetColor();
            _console.WriteLine();
        }
    }

    private void ApplyColors(ColorState colorState)
    {
        switch (colorState)
        {
            case ColorState.Warn warn:
                _console.ForegroundColor = warn.Foreground;
                if (warn.Background.HasValue)
                    _console.BackgroundColor = warn.Background.Value;
                break;

            case ColorState.Reboot reboot:
                _console.ForegroundColor = reboot.Foreground;
                if (reboot.Background.HasValue)
                    _console.BackgroundColor = reboot.Background.Value;
                break;

            case ColorState.Overdue overdue:
                _console.ForegroundColor = overdue.ActivePair.Foreground;
                _console.BackgroundColor = overdue.ActivePair.Background;
                break;

            case ColorState.Default:
            default:
                // No color change for default state
                break;
        }
    }
}
