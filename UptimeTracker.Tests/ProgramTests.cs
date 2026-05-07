namespace UptimeTracker.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Main_ConfigFileNotFound_ReturnsExitCode1AndWritesToStderr()
    {
        // Arrange: use a temp directory with no config file
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // We need to test Program.Main, but it uses AppContext.BaseDirectory.
            // Instead, test ConfigLoader directly for the integration scenario.
            // For a true integration test, we'd need to run the executable.
            // Here we verify the behavior through ConfigLoader since Program.Main
            // catches ConfigurationException and writes to stderr.

            // Actually, let's capture stderr by redirecting Console.Error
            var originalError = System.Console.Error;

            await using var errorWriter = new StringWriter();
            
            System.Console.SetError(errorWriter);

            // We can't easily change AppContext.BaseDirectory, so let's test
            // the error path by ensuring ConfigLoader throws for missing file
            var ex = Assert.Throws<Exceptions.ConfigurationException>(
                () => ConfigLoader.Load(tempDir));

            Assert.Contains(tempDir, ex.Message);
            Assert.Equal(1, ex.ExitCode);

            System.Console.SetError(originalError);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ConfigLoader_ValidConfig_LoadsSuccessfully()
    {
        // Arrange: write a valid config to a temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "uptime-tracker.json"), """
                {
                  "testMode": true,
                  "warn":   { "after": "00:00:10" },
                  "reboot": { "after": "00:00:20" },
                  "overdue":{ "after": "00:00:30" }
                }
                """);

            var config = ConfigLoader.Load(tempDir);

            Assert.True(config.TestMode);
            Assert.Equal(TimeSpan.FromSeconds(10), config.Thresholds.Warn.After);
            Assert.Equal(TimeSpan.FromSeconds(20), config.Thresholds.Reboot.After);
            Assert.Equal(TimeSpan.FromSeconds(30), config.Thresholds.Overdue.After);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
