namespace UptimeTracker.Models;

internal sealed record ThresholdConfiguration(WarnThreshold Warn, RebootThreshold Reboot, OverdueThreshold Overdue);
