namespace UptimeTracker.BootTime;

internal interface IBootTimeProvider
{
    /// <summary>Returns the local date/time at which the OS last booted.</summary>
    DateTime GetBootTime();
}
