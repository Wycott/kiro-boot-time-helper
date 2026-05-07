namespace UptimeTracker.Models;

internal abstract record ColorState
{
    public sealed record Default : ColorState;
    public sealed record Warn(ConsoleColor Foreground, ConsoleColor? Background) : ColorState;
    public sealed record Reboot(ConsoleColor Foreground, ConsoleColor? Background) : ColorState;
    public sealed record Overdue(FlashPair ActivePair) : ColorState;
}
