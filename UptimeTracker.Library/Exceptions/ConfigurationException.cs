namespace UptimeTracker.Exceptions;

internal sealed class ConfigurationException : Exception
{
    public int ExitCode { get; } = 1;

    public ConfigurationException(string message) : base(message) { }
}
