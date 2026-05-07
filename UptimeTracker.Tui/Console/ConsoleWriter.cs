namespace UptimeTracker.Console;

internal sealed class ConsoleWriter : IConsoleWriter
{
    public int CursorTop => System.Console.CursorTop;

    public void SetCursorPosition(int left, int top) => System.Console.SetCursorPosition(left, top);
    public void Write(string value) => System.Console.Write(value);
    public void WriteLine(string value) => System.Console.WriteLine(value);
    public void WriteLine() => System.Console.WriteLine();

    public ConsoleColor ForegroundColor
    {
        get => System.Console.ForegroundColor;
        set => System.Console.ForegroundColor = value;
    }
    
    public ConsoleColor BackgroundColor
    {
        get => System.Console.BackgroundColor;
        set => System.Console.BackgroundColor = value;
    }
    
    public void ResetColor() => System.Console.ResetColor();
}
