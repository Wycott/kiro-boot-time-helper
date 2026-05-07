using UptimeTracker.BootTime;
using UptimeTracker.Console;
using UptimeTracker.Exceptions;
using static System.Console;

namespace UptimeTracker;

internal sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Capture program start time before any other work
        var programStartTime = DateTime.Now;

        try
        {
            // 1. Load and validate configuration
            var executableDirectory = AppContext.BaseDirectory;
            var appConfig = ConfigLoader.Load(executableDirectory);

            // 2. Select IBootTimeProvider based on testMode
            IBootTimeProvider bootTimeProvider = appConfig.TestMode
                ? new TestBootTimeProvider(programStartTime)
                : new SystemBootTimeProvider();

            // 3. Save original console colors
            var originalForeground = ForegroundColor;
            var originalBackground = BackgroundColor;

            // 4. Clear the screen
            Clear();

            // 5. Register Ctrl+C handler
            using var cts = new CancellationTokenSource();
            CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // Prevent immediate termination
                cts.Cancel();
            };

            // 5. Print boot-time line (static, never refreshed)
            var bootTime = bootTimeProvider.GetBootTime();
            var bootTimeLine = $"Boot time: {bootTime:yyyy-MM-dd HH:mm:ss}";

            if (appConfig.TestMode)
            {
                bootTimeLine += " [TEST MODE]";
            }

            WriteLine(bootTimeLine);

            // 6. Show configuration summary with styled previews
            PrintConfigSummary(appConfig);

            // 7. Enter render loop
            var consoleWriter = new ConsoleWriter();
            var renderer = new UptimeRenderer(
                bootTimeProvider,
                appConfig.Thresholds,
                consoleWriter,
                appConfig.TestMode);

            await renderer.RunAsync(cts.Token);

            // 8. Restore original console colors (belt and suspenders — renderer also resets)
            ForegroundColor = originalForeground;
            BackgroundColor = originalBackground;

            return 0;
        }
        catch (ConfigurationException ex)
        {
            Error.WriteLine(ex.Message);

            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Error.WriteLine($"Error: Unable to retrieve system boot time: {ex.Message}");

            return 1;
        }
    }

    private static void PrintConfigSummary(Models.AppConfiguration appConfig)
    {
        var thresholds = appConfig.Thresholds;

        WriteLine();
        WriteLine("Configuration:");
        WriteLine($"  Test mode: {(appConfig.TestMode ? "ON" : "OFF")}");
        WriteLine();

        // Warn threshold
        Write($"  Warn     (after {FormatThreshold(thresholds.Warn.After)}): ");
        ForegroundColor = thresholds.Warn.Foreground;
        if (thresholds.Warn.Background.HasValue)
        {
            BackgroundColor = thresholds.Warn.Background.Value;
        }
        Write("SAMPLE TEXT");
        ResetColor();
        WriteLine();

        // Reboot threshold
        Write($"  Reboot   (after {FormatThreshold(thresholds.Reboot.After)}): ");
        ForegroundColor = thresholds.Reboot.Foreground;
        if (thresholds.Reboot.Background.HasValue)
        {
            BackgroundColor = thresholds.Reboot.Background.Value;
        }
        Write("SAMPLE TEXT");
        ResetColor();
        WriteLine();

        // Overdue threshold
        Write($"  Overdue  (after {FormatThreshold(thresholds.Overdue.After)}, flash {thresholds.Overdue.FlashIntervalMs}ms): ");
        ForegroundColor = thresholds.Overdue.PairA.Foreground;
        BackgroundColor = thresholds.Overdue.PairA.Background;
        Write("FLASH");
        ResetColor();
        Write(" / ");
        ForegroundColor = thresholds.Overdue.PairB.Foreground;
        BackgroundColor = thresholds.Overdue.PairB.Background;
        Write("FLASH");
        ResetColor();
        WriteLine();

        WriteLine();
    }

    private static string FormatThreshold(TimeSpan ts)
    {
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
