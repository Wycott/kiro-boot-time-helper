namespace UptimeTracker.Models;

internal sealed record OverdueThreshold(TimeSpan After, FlashPair PairA, FlashPair PairB, int FlashIntervalMs);
