namespace QTSAvalonia.CLI;

public static class CliLogger
{
    private static bool _verbose;

    public static void SetVerbose(bool verbose)
    {
        _verbose = verbose;
    }

    public static void Info(string message)
    {
        Console.Error.WriteLine(_verbose ? $"[INFO] {message}" : message);
    }

    public static void Warning(string message)
    {
        Console.Error.WriteLine($"[WARN] {message}");
    }

    public static void Error(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }

    public static void Progress(int current, int total, string message)
    {
        Console.Error.WriteLine($"[{current}/{total}] {message}");
    }

    public static void Done(int count, TimeSpan elapsed)
    {
        Console.Error.WriteLine($"Done. {count} file(s) processed in {elapsed.TotalSeconds:F1}s");
    }

    public static void Start(string command, string input)
    {
        Console.Error.WriteLine($"Starting {command}: {input}");
    }
}