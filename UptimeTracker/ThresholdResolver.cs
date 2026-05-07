using UptimeTracker.Models;

namespace UptimeTracker;

internal static class ThresholdResolver
{
    public static ColorState Resolve(TimeSpan uptime, ThresholdConfiguration config, int flashTick)
    {
        if (uptime >= config.Overdue.After)
        {
            FlashPair activePair = flashTick % 2 == 0 ? config.Overdue.PairA : config.Overdue.PairB;
            return new ColorState.Overdue(activePair);
        }

        if (uptime >= config.Reboot.After)
            return new ColorState.Reboot(config.Reboot.Foreground, config.Reboot.Background);

        if (uptime >= config.Warn.After)
            return new ColorState.Warn(config.Warn.Foreground, config.Warn.Background);

        return new ColorState.Default();
    }
}
