using UptimeTracker.BootTime;
using UptimeTracker.Console;
using UptimeTracker.Exceptions;
using UptimeTracker.Models;

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
                bootTimeLine += " [TEST MODE]";
            System.Console.WriteLine(bootTimeLine);

            // 6. Enter render loop
            var consoleWriter = new ConsoleWriter();
            var renderer = new UptimeRenderer(
                bootTimeProvider,
                appConfig.Thresholds,
                consoleWriter,
                appConfig.TestMode);

            await renderer.RunAsync(cts.Token);

            // 7. Restore original console colors (belt and suspenders — renderer also resets)
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
}
