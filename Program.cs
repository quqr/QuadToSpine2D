using Avalonia;
using QTSAvalonia.CLI;

namespace QTSAvalonia;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (CliRoot.TryRunCli(args))
            return;

        CliRoot.StartGui(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return CliRoot.BuildAvaloniaApp();
    }
}