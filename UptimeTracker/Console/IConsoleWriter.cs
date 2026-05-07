namespace UptimeTracker.Console;

internal interface IConsoleWriter
{
    void SetCursorPosition(int left, int top);
    int CursorTop { get; }
    void Write(string value);
    void WriteLine(string value);
    void WriteLine();
    ConsoleColor ForegroundColor { get; set; }
    ConsoleColor BackgroundColor { get; set; }
    void ResetColor();
}
