// Feature: dotnet-uptime-tracker, Property 1: Uptime format round-trip

using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using UptimeTracker;

namespace UptimeTracker.Tests;

public class UptimeFormatterPropertyTests
{
    /// <summary>
    /// Generator: random TimeSpan in [0, 999 days] truncated to whole seconds.
    /// </summary>
    private static Arbitrary<TimeSpan> UptimeArb =>
        ArbMap.Default.ArbFor<int>()
              .Generator
              .Select(n => TimeSpan.FromSeconds(Math.Abs(n % (999 * 24 * 60 * 60 + 1))))
              .ToArbitrary();

    /// <summary>
    /// Validates: Requirements 2.2
    ///
    /// For any TimeSpan in [0, 999 days] (whole seconds), parsing the output of
    /// UptimeFormatter.Format back into components must exactly match the original
    /// TimeSpan's days, hours, minutes, and seconds.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UptimeFormatRoundTrip()
    {
        return Prop.ForAll(
            UptimeArb,
            t =>
            {
                string formatted = UptimeFormatter.Format(t);

                // Parse "Xd HHh MMm SSs"
                var match = Regex.Match(formatted, @"^(\d+)d (\d{2})h (\d{2})m (\d{2})s$");

                if (!match.Success)
                {
                    return false;
                }

                var parsedDays    = int.Parse(match.Groups[1].Value);
                var parsedHours   = int.Parse(match.Groups[2].Value);
                var parsedMinutes = int.Parse(match.Groups[3].Value);
                var parsedSeconds = int.Parse(match.Groups[4].Value);

                return parsedDays    == (int)t.TotalDays
                    && parsedHours   == t.Hours
                    && parsedMinutes == t.Minutes
                    && parsedSeconds == t.Seconds;
            });
    }
}
