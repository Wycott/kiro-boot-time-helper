using UptimeTracker.Console;

namespace UptimeTracker.Tests.TestHelpers;

/// <summary>
/// A fake implementation of IConsoleWriter that records all calls for test assertions.
/// </summary>
internal sealed class FakeConsoleWriter : IConsoleWriter
{
    // Configurable CursorTop value (default 1, simulating being on line 1 after boot time line)
    public int CursorTopValue { get; set; } = 1;

    public int CursorTop => CursorTopValue;

    // Recorded state
    public List<string> WrittenTexts { get; } = new();
    public List<string> WrittenLines { get; } = new();
    public List<(int Left, int Top)> SetCursorPositionCalls { get; } = new();
    public int ResetColorCallCount { get; private set; }
    public List<ConsoleColor> ForegroundColors { get; } = new();
    public List<ConsoleColor> BackgroundColors { get; } = new();

    // Current color state
    private ConsoleColor foregroundColor = ConsoleColor.Gray;
    private ConsoleColor backgroundColor = ConsoleColor.Black;

    public ConsoleColor ForegroundColor
    {
        get => foregroundColor;
        set
        {
            foregroundColor = value;
            ForegroundColors.Add(value);
        }
    }

    public ConsoleColor BackgroundColor
    {
        get => backgroundColor;
        set
        {
            backgroundColor = value;
            BackgroundColors.Add(value);
        }
    }

    public void SetCursorPosition(int left, int top)
    {
        SetCursorPositionCalls.Add((left, top));
    }

    public void Write(string value)
    {
        WrittenTexts.Add(value);
    }

    public void WriteLine(string value)
    {
        WrittenLines.Add(value);
    }

    public void WriteLine()
    {
        WrittenLines.Add(string.Empty);
    }

    public void ResetColor()
    {
        ResetColorCallCount++;
    }
}
