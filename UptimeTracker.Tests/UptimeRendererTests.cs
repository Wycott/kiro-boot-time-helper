using UptimeTracker;
using UptimeTracker.BootTime;
using UptimeTracker.Models;
using UptimeTracker.Tests.TestHelpers;

namespace UptimeTracker.Tests;

public class UptimeRendererTests
{
    // Standard ThresholdConfiguration with small values for testing
    // warn=1s, reboot=2s, overdue=3s
    private static ThresholdConfiguration MakeConfig(int flashIntervalMs = 500) =>
        new ThresholdConfiguration(
            Warn: new WarnThreshold(
                After: TimeSpan.FromSeconds(1),
                Foreground: ConsoleColor.Yellow,
                Background: null),
            Reboot: new RebootThreshold(
                After: TimeSpan.FromSeconds(2),
                Foreground: ConsoleColor.Red,
                Background: null),
            Overdue: new OverdueThreshold(
                After: TimeSpan.FromSeconds(3),
                PairA: new FlashPair(ConsoleColor.Red, ConsoleColor.White),
                PairB: new FlashPair(ConsoleColor.White, ConsoleColor.Red),
                FlashIntervalMs: flashIntervalMs));

    /// <summary>
    /// Creates a delayFunc that cancels after a given number of delay calls.
    /// This lets us control exactly how many render iterations occur.
    /// </summary>
    private static (Func<int, CancellationToken, Task> delayFunc, CancellationTokenSource cts)
        MakeControlledDelay(int cancelAfterIterations)
    {
        var cts = new CancellationTokenSource();
        int count = 0;
        Task DelayFunc(int ms, CancellationToken ct)
        {
            count++;
            if (count >= cancelAfterIterations)
                cts.Cancel();
            return Task.CompletedTask;
        }
        return (DelayFunc, cts);
    }

    // ─── Cursor position tests ───────────────────────────────────────────────

    [Fact]
    public async Task Renderer_RecordsCursorTopAtStart()
    {
        // Arrange
        var console = new FakeConsoleWriter { CursorTopValue = 5 };
        var bootProvider = new TestBootTimeProvider(DateTime.Now); // uptime ~0 → Default state
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: SetCursorPosition was called with the saved row (5)
        Assert.Contains(console.SetCursorPositionCalls, call => call.Top == 5);
    }

    [Fact]
    public async Task Renderer_AfterFirstRender_SetCursorPositionCalledWithColumn0()
    {
        // Arrange
        var console = new FakeConsoleWriter { CursorTopValue = 3 };
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: first SetCursorPosition call uses column 0 and the saved row
        Assert.NotEmpty(console.SetCursorPositionCalls);
        Assert.Equal(0, console.SetCursorPositionCalls[0].Left);
        Assert.Equal(3, console.SetCursorPositionCalls[0].Top);
    }

    // ─── In-place rendering tests ─────────────────────────────────────────────

    [Fact]
    public async Task Renderer_WritesUptimeViaWrite_NotWriteLine()
    {
        // Arrange
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: uptime is written via Write(), not WriteLine()
        // WrittenTexts should have the uptime string; WrittenLines should only have the cleanup empty line
        Assert.NotEmpty(console.WrittenTexts);
        // The only WriteLine() call should be the final cleanup empty line
        Assert.Single(console.WrittenLines);
        Assert.Equal(string.Empty, console.WrittenLines[0]);
    }

    [Fact]
    public async Task Renderer_MultipleIterations_SetCursorPositionCalledEachTime()
    {
        // Arrange
        var console = new FakeConsoleWriter { CursorTopValue = 2 };
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(3); // 3 iterations

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: SetCursorPosition called once per iteration (3 times)
        Assert.Equal(3, console.SetCursorPositionCalls.Count);
        Assert.All(console.SetCursorPositionCalls, call =>
        {
            Assert.Equal(0, call.Left);
            Assert.Equal(2, call.Top);
        });
    }

    // ─── Color state tests ────────────────────────────────────────────────────

    [Fact]
    public async Task Renderer_DefaultState_NoColorAssignmentsBeforeWrite()
    {
        // Arrange: boot time = now, so uptime ≈ 0 → below warn (1s) → Default state
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: no foreground/background colors set (only ResetColor called)
        Assert.Empty(console.ForegroundColors);
        Assert.Empty(console.BackgroundColors);
        Assert.True(console.ResetColorCallCount >= 1);
    }

    [Fact]
    public async Task Renderer_WarnState_SetsForegroundToWarnColor()
    {
        // Arrange: boot time = 1.5s ago → uptime ≈ 1.5s → >= warn(1s), < reboot(2s) → Warn state
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now - TimeSpan.FromSeconds(1.5));
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: ForegroundColor set to Yellow (warn foreground)
        Assert.NotEmpty(console.ForegroundColors);
        Assert.Equal(ConsoleColor.Yellow, console.ForegroundColors[0]);
        Assert.Empty(console.BackgroundColors); // no background for warn (null)
        Assert.True(console.ResetColorCallCount >= 1);
    }

    [Fact]
    public async Task Renderer_RebootState_SetsForegroundToRebootColor()
    {
        // Arrange: boot time = 2.5s ago → uptime ≈ 2.5s → >= reboot(2s), < overdue(3s) → Reboot state
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now - TimeSpan.FromSeconds(2.5));
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: ForegroundColor set to Red (reboot foreground)
        Assert.NotEmpty(console.ForegroundColors);
        Assert.Equal(ConsoleColor.Red, console.ForegroundColors[0]);
        Assert.Empty(console.BackgroundColors); // no background for reboot (null)
        Assert.True(console.ResetColorCallCount >= 1);
    }

    [Fact]
    public async Task Renderer_OverdueState_EvenFlashTick_SetsPairAColors()
    {
        // Arrange: boot time = 4s ago → uptime ≈ 4s → >= overdue(3s) → Overdue state
        // flashTick starts at 0 (even) → PairA: Red fg / White bg
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now - TimeSpan.FromSeconds(4));
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: PairA colors (Red fg, White bg)
        Assert.NotEmpty(console.ForegroundColors);
        Assert.Equal(ConsoleColor.Red, console.ForegroundColors[0]);
        Assert.NotEmpty(console.BackgroundColors);
        Assert.Equal(ConsoleColor.White, console.BackgroundColors[0]);
    }

    [Fact]
    public async Task Renderer_OverdueState_OddFlashTick_SetsPairBColors()
    {
        // Arrange: boot time = 4s ago → uptime ≈ 4s → >= overdue(3s) → Overdue state
        // Run 2 iterations: flashTick=0 (PairA), flashTick=1 (PairB)
        // Cancel after 2nd delay → we get 2 renders, second uses flashTick=1 (odd) → PairB
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now - TimeSpan.FromSeconds(4));
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(2);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: 2 renders occurred, second render used PairB (White fg, Red bg)
        // ForegroundColors: [Red (PairA), White (PairB)]
        // BackgroundColors: [White (PairA), Red (PairB)]
        Assert.Equal(2, console.ForegroundColors.Count);
        Assert.Equal(ConsoleColor.White, console.ForegroundColors[1]); // PairB foreground
        Assert.Equal(2, console.BackgroundColors.Count);
        Assert.Equal(ConsoleColor.Red, console.BackgroundColors[1]); // PairB background
    }

    // ─── Cancellation / cleanup tests ─────────────────────────────────────────

    [Fact]
    public async Task Renderer_OnCancellation_ResetColorCalledInFinally()
    {
        // Arrange
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: ResetColor called at least once (in finally block)
        Assert.True(console.ResetColorCallCount >= 1);
    }

    [Fact]
    public async Task Renderer_OnCancellation_WritesEmptyLineInFinally()
    {
        // Arrange
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(1);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: final WriteLine() (empty) called in finally block
        Assert.Contains(console.WrittenLines, line => line == string.Empty);
    }

    [Fact]
    public async Task Renderer_AfterCancellation_NoFurtherWriteCalls()
    {
        // Arrange: cancel after 2 iterations, verify Write count matches exactly 2
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var (delayFunc, cts) = MakeControlledDelay(2);

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: delayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: exactly 2 Write calls (one per iteration), no more
        Assert.Equal(2, console.WrittenTexts.Count);
    }

    [Fact]
    public async Task Renderer_ExternalCancellation_StopsLoop()
    {
        // Arrange: cancel externally before any delay completes
        var console = new FakeConsoleWriter();
        var bootProvider = new TestBootTimeProvider(DateTime.Now);
        var config = MakeConfig();
        var cts = new CancellationTokenSource();

        // delayFunc that cancels immediately on first call
        Task DelayFunc(int ms, CancellationToken ct)
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: DelayFunc);

        // Act
        await renderer.RunAsync(cts.Token);

        // Assert: cleanup happened (ResetColor + empty WriteLine)
        Assert.True(console.ResetColorCallCount >= 1);
        Assert.Contains(console.WrittenLines, line => line == string.Empty);
    }
}
