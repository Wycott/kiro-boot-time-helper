namespace UptimeTracker.Exceptions;

internal sealed class ConfigurationException(string message) : Exception(message)
{
    public int ExitCode { get; } = 1;
}
