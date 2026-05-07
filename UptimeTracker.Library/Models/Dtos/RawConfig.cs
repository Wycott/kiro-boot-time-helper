namespace UptimeTracker.Models.Dtos;

internal sealed record RawConfig(bool? TestMode, RawThreshold? Warn, RawThreshold? Reboot, RawThreshold? Overdue);
