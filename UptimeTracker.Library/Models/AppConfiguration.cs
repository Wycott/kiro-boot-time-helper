namespace UptimeTracker.Models;

internal sealed record AppConfiguration(ThresholdConfiguration Thresholds, bool TestMode);
