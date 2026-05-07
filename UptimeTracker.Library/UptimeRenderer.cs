using UptimeTracker.BootTime;
using UptimeTracker.Console;
using UptimeTracker.Models;

namespace UptimeTracker;

internal sealed class UptimeRenderer
{
    private readonly IBootTimeProvider bootTimeProvider;
    private readonly ThresholdConfiguration config;
    private readonly IConsoleWriter console;
    private readonly bool testMode;
    private readonly Func<int, CancellationToken, Task>? delayFunc;

    public UptimeRenderer(
        IBootTimeProvider bootTimeProvider,
        ThresholdConfiguration config,
        IConsoleWriter console,
        bool testMode = false,
        Func<int, CancellationToken, Task>? delayFunc = null)
    {
        this.bootTimeProvider = bootTimeProvider;
        this.config = config;
        this.console = console;
        this.testMode = testMode;
        this.delayFunc = delayFunc;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Hide the cursor
        console.Write("\x1b[?25l");

        // Record the cursor row where uptime will be written in-place
        var uptimeRow = console.CursorTop;

        var flashTick = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bootTime = bootTimeProvider.GetBootTime();
                var uptime = DateTime.Now - bootTime;
                var colorState = ThresholdResolver.Resolve(uptime, config, flashTick);

                // Move cursor to the uptime line (in-place update)
                console.SetCursorPosition(0, uptimeRow);

                // Write "Uptime:" label in bold (ANSI escape), then the value in color
                console.Write("\x1b[1mUptime:\x1b[0m ");
                ApplyColors(colorState);
                console.Write($"{UptimeFormatter.Format(uptime)}   ");
                console.ResetColor();

                flashTick++;

                // Determine delay based on state
                var delayMs = colorState is ColorState.Overdue
                    ? config.Overdue.FlashIntervalMs
                    : 1000;

                try
                {
                    if (this.delayFunc != null)
                    {
                        await this.delayFunc(delayMs, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            // Cleanup: show cursor, restore colors and print final newline
            console.Write("\x1b[?25h");
            console.ResetColor();
            console.WriteLine();
        }
    }

    private void ApplyColors(ColorState colorState)
    {
        switch (colorState)
        {
            case ColorState.Warn warn:
                console.ForegroundColor = warn.Foreground;
                if (warn.Background.HasValue)
                {
                    console.BackgroundColor = warn.Background.Value;
                }
                break;

            case ColorState.Reboot reboot:
                console.ForegroundColor = reboot.Foreground;
                if (reboot.Background.HasValue)
                {
                    console.BackgroundColor = reboot.Background.Value;
                }
                break;

            case ColorState.Overdue overdue:
                console.ForegroundColor = overdue.ActivePair.Foreground;
                console.BackgroundColor = overdue.ActivePair.Background;
                break;

            case ColorState.Default:
            default:
                // No color change for default state
                break;
        }
    }
}
