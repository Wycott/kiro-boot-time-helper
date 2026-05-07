namespace UptimeTracker.Models;

internal sealed record WarnThreshold(TimeSpan After, ConsoleColor Foreground, ConsoleColor? Background);
