// Feature: dotnet-uptime-tracker, Property 7: Render loop uses configured flash interval

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using UptimeTracker;
using UptimeTracker.BootTime;
using UptimeTracker.Models;
using UptimeTracker.Tests.TestHelpers;

namespace UptimeTracker.Tests;

/// <summary>
/// Property 7: Render loop uses configured flash interval.
///
/// Validates: Requirements 6.4, 6.7
///
/// For any valid flashIntervalMs in [1, 10000], when the renderer is in overdue state,
/// the delay used SHALL equal flashIntervalMs.
/// </summary>
public class UptimeRendererPropertyTests
{
    /// <summary>
    /// Generator: random flashIntervalMs in [1, 10000].
    /// </summary>
    private static Arbitrary<int> FlashIntervalArb =>
        ArbMap.Default.ArbFor<int>()
              .Generator
              .Select(n => Math.Abs(n % 10000) + 1) // [1, 10000]
              .ToArbitrary();

    /// <summary>
    /// Validates: Requirements 6.4, 6.7
    ///
    /// For any flashIntervalMs in [1, 10000], when the renderer is in overdue state,
    /// the delay passed to the delay function SHALL equal flashIntervalMs.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property OverdueState_UsesConfiguredFlashInterval()
    {
        return Prop.ForAll(
            FlashIntervalArb,
            flashIntervalMs =>
            {
                // Build a config where uptime will be in overdue state
                // warn=1s, reboot=2s, overdue=3s, with the given flashIntervalMs
                var config = new ThresholdConfiguration(
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

                // Boot time 4 seconds ago → uptime ≈ 4s → overdue state
                var bootProvider = new TestBootTimeProvider(DateTime.Now - TimeSpan.FromSeconds(4));
                var console = new FakeConsoleWriter();

                // Capture delay values
                var recordedDelays = new List<int>();
                var cts = new CancellationTokenSource();
                int callCount = 0;

                Task DelayFunc(int ms, CancellationToken ct)
                {
                    recordedDelays.Add(ms);
                    callCount++;
                    if (callCount >= 1)
                        cts.Cancel();
                    return Task.CompletedTask;
                }

                var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: DelayFunc);

                // Run synchronously (all delays are instant)
                renderer.RunAsync(cts.Token).GetAwaiter().GetResult();

                // Assert: the delay used equals flashIntervalMs (overdue state)
                return recordedDelays.Count > 0
                    && recordedDelays.All(d => d == flashIntervalMs);
            });
    }

    /// <summary>
    /// Validates: Requirements 6.4, 6.7
    ///
    /// When NOT in overdue state, the delay SHALL be 1000ms regardless of flashIntervalMs.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NonOverdueState_Uses1000msDelay()
    {
        return Prop.ForAll(
            FlashIntervalArb,
            flashIntervalMs =>
            {
                // Build a config where uptime will be in Default state (below warn)
                var config = new ThresholdConfiguration(
                    Warn: new WarnThreshold(
                        After: TimeSpan.FromSeconds(10),
                        Foreground: ConsoleColor.Yellow,
                        Background: null),
                    Reboot: new RebootThreshold(
                        After: TimeSpan.FromSeconds(20),
                        Foreground: ConsoleColor.Red,
                        Background: null),
                    Overdue: new OverdueThreshold(
                        After: TimeSpan.FromSeconds(30),
                        PairA: new FlashPair(ConsoleColor.Red, ConsoleColor.White),
                        PairB: new FlashPair(ConsoleColor.White, ConsoleColor.Red),
                        FlashIntervalMs: flashIntervalMs));

                // Boot time = now → uptime ≈ 0 → Default state (below warn at 10s)
                var bootProvider = new TestBootTimeProvider(DateTime.Now);
                var console = new FakeConsoleWriter();

                var recordedDelays = new List<int>();
                var cts = new CancellationTokenSource();
                int callCount = 0;

                Task DelayFunc(int ms, CancellationToken ct)
                {
                    recordedDelays.Add(ms);
                    callCount++;
                    if (callCount >= 1)
                        cts.Cancel();
                    return Task.CompletedTask;
                }

                var renderer = new UptimeRenderer(bootProvider, config, console, delayFunc: DelayFunc);
                renderer.RunAsync(cts.Token).GetAwaiter().GetResult();

                // Assert: delay is 1000ms (not flashIntervalMs) when not in overdue state
                return recordedDelays.Count > 0
                    && recordedDelays.All(d => d == 1000);
            });
    }
}
