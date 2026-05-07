namespace UptimeTracker.Models;

internal sealed record RebootThreshold(TimeSpan After, ConsoleColor Foreground, ConsoleColor? Background);
