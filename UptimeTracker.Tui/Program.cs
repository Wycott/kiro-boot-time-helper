using UptimeTracker.BootTime;
using UptimeTracker.Console;
using UptimeTracker.Exceptions;

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
            var originalForeground = System.Console.ForegroundColor;
            var originalBackground = System.Console.BackgroundColor;

            // 4. Register Ctrl+C handler
            using var cts = new CancellationTokenSource();
            System.Console.CancelKeyPress += (_, e) =>
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

            System.Console.WriteLine(bootTimeLine);

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
            System.Console.ForegroundColor = originalForeground;
            System.Console.BackgroundColor = originalBackground;

            return 0;
        }
        catch (ConfigurationException ex)
        {
            System.Console.Error.WriteLine(ex.Message);

            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Error: Unable to retrieve system boot time: {ex.Message}");

            return 1;
        }
    }

    private static void PrintConfigSummary(Models.AppConfiguration appConfig)
    {
        var thresholds = appConfig.Thresholds;

        System.Console.WriteLine();
        System.Console.WriteLine("Configuration:");
        System.Console.WriteLine($"  Test mode: {(appConfig.TestMode ? "ON" : "OFF")}");
        System.Console.WriteLine();

        // Warn threshold
        System.Console.Write($"  Warn     (after {FormatThreshold(thresholds.Warn.After)}): ");
        System.Console.ForegroundColor = thresholds.Warn.Foreground;
        if (thresholds.Warn.Background.HasValue)
        {
            System.Console.BackgroundColor = thresholds.Warn.Background.Value;
        }
        System.Console.Write("SAMPLE TEXT");
        System.Console.ResetColor();
        System.Console.WriteLine();

        // Reboot threshold
        System.Console.Write($"  Reboot   (after {FormatThreshold(thresholds.Reboot.After)}): ");
        System.Console.ForegroundColor = thresholds.Reboot.Foreground;
        if (thresholds.Reboot.Background.HasValue)
        {
            System.Console.BackgroundColor = thresholds.Reboot.Background.Value;
        }
        System.Console.Write("SAMPLE TEXT");
        System.Console.ResetColor();
        System.Console.WriteLine();

        // Overdue threshold
        System.Console.Write($"  Overdue  (after {FormatThreshold(thresholds.Overdue.After)}, flash {thresholds.Overdue.FlashIntervalMs}ms): ");
        System.Console.ForegroundColor = thresholds.Overdue.PairA.Foreground;
        System.Console.BackgroundColor = thresholds.Overdue.PairA.Background;
        System.Console.Write("FLASH");
        System.Console.ResetColor();
        System.Console.Write(" / ");
        System.Console.ForegroundColor = thresholds.Overdue.PairB.Foreground;
        System.Console.BackgroundColor = thresholds.Overdue.PairB.Background;
        System.Console.Write("FLASH");
        System.Console.ResetColor();
        System.Console.WriteLine();

        System.Console.WriteLine();
    }

    private static string FormatThreshold(TimeSpan ts)
    {
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
