// Feature: dotnet-uptime-tracker, Property 2: Threshold resolver — highest threshold wins
// Feature: dotnet-uptime-tracker, Property 3: Flash alternation within two Flash_Interval periods

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using UptimeTracker.Models;

namespace UptimeTracker.Tests;

public class ThresholdResolverPropertyTests
{
    // ── Generators ───────────────────────────────────────────────────────────────

    /// <summary>Random TimeSpan in [0, 30 days] (whole seconds).</summary>
    private static Arbitrary<TimeSpan> UptimeArb =>
        ArbMap.Default.ArbFor<int>()
              .Generator
              .Select(n => TimeSpan.FromSeconds(Math.Abs(n % (30 * 24 * 3600 + 1))))
              .ToArbitrary();

    /// <summary>
    /// Random valid ThresholdConfiguration with three strictly ascending After values.
    /// Generates three distinct second offsets in [1, 30 days] and sorts them.
    /// </summary>
    private static Arbitrary<ThresholdConfiguration> ThresholdConfigArb
    {
        get
        {
            // Generate three positive second values and sort them to guarantee ascending order
            var gen =
                from a in Gen.Choose(1, 10 * 3600)          // 1s – 10h
                from b in Gen.Choose(a + 1, 20 * 3600)      // a+1s – 20h
                from c in Gen.Choose(b + 1, 30 * 3600)      // b+1s – 30h
                select new ThresholdConfiguration(
                    Warn: new WarnThreshold(
                        After: TimeSpan.FromSeconds(a),
                        Foreground: ConsoleColor.Yellow,
                        Background: null),
                    Reboot: new RebootThreshold(
                        After: TimeSpan.FromSeconds(b),
                        Foreground: ConsoleColor.Red,
                        Background: null),
                    Overdue: new OverdueThreshold(
                        After: TimeSpan.FromSeconds(c),
                        PairA: new FlashPair(ConsoleColor.Red, ConsoleColor.White),
                        PairB: new FlashPair(ConsoleColor.White, ConsoleColor.Red),
                        FlashIntervalMs: 1000));

            return gen.ToArbitrary();
        }
    }

    // ── Property 2: Highest threshold wins ──────────────────────────────────────

    /// <summary>
    /// Validates: Requirements 4.1, 5.1, 6.1, 6.6
    ///
    /// For any uptime and valid ThresholdConfiguration, ThresholdResolver.Resolve
    /// returns the correct ColorState based on which threshold range the uptime falls in.
    /// The four ranges are mutually exclusive and exhaustive.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HighestThresholdWins()
    {
        var arb = UptimeArb.Generator
            .Zip(ThresholdConfigArb.Generator)
            .ToArbitrary();

        return Prop.ForAll(arb, pair =>
        {
            var (uptime, config) = pair;
            var result = ThresholdResolver.Resolve(uptime, config, 0);

            if (uptime >= config.Overdue.After)
            {
                return result is ColorState.Overdue;
            }

            if (uptime >= config.Reboot.After)
            {
                return result is ColorState.Reboot;
            }

            if (uptime >= config.Warn.After)
            {
                return result is ColorState.Warn;
            }

            return result is ColorState.Default;
        });
    }

    // ── Property 3: Flash alternation correctness ────────────────────────────────

    /// <summary>
    /// Validates: Requirements 6.4
    ///
    /// For any sequence of consecutive flashTick integers with uptime in overdue state,
    /// flashTick % 2 == 0 always yields PairA and flashTick % 2 == 1 always yields PairB.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FlashAlternationCorrectness()
    {
        // Generator: starting tick (non-negative) and sequence length (2–20)
        var arb =
            (from start in Gen.Choose(0, int.MaxValue / 2)
             from length in Gen.Choose(2, 20)
             select (start, length))
            .ToArbitrary();

        // Use a fixed config where overdue.After = 1s so any reasonable uptime qualifies
        var config = new ThresholdConfiguration(
            Warn: new WarnThreshold(TimeSpan.FromSeconds(1), ConsoleColor.Yellow, null),
            Reboot: new RebootThreshold(TimeSpan.FromSeconds(2), ConsoleColor.Red, null),
            Overdue: new OverdueThreshold(
                After: TimeSpan.FromSeconds(3),
                PairA: new FlashPair(ConsoleColor.Red, ConsoleColor.White),
                PairB: new FlashPair(ConsoleColor.White, ConsoleColor.Red),
                FlashIntervalMs: 1000));

        var overdueUptime = TimeSpan.FromSeconds(10);

        return Prop.ForAll(arb, pair =>
        {
            var (start, length) = pair;

            for (var i = 0; i < length; i++)
            {
                var flashTick = start + i;
                var result = (ColorState.Overdue)ThresholdResolver.Resolve(overdueUptime, config, flashTick);

                if (flashTick % 2 == 0)
                {
                    if (!result.ActivePair.Equals(config.Overdue.PairA))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!result.ActivePair.Equals(config.Overdue.PairB))
                    {
                        return false;
                    }
                }
            }

            return true;
        });
    }
}
