using Avalonia;

namespace QTSAvalonia.CLI;

public static class CliRoot
{
    private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "tex", "t", "decode", "td",
        "sprite", "sp", "parse", "toquad", "sq",
        "spine", "s2",
        "convert", "c",
        "help", "--help", "-h"
    };

    public static bool TryRunCli(string[] args)
    {
        if (args.Length == 0)
            return false;

        var command = args[0].ToLowerInvariant();

        if (!IsKnownCommand(command))
        {
            PrintMainHelp();
            return false;
        }

        return command switch
        {
            "tex" or "t" => RunTexCommand(args[1..]),
            "decode" or "td" => RunDecode(args[1..]),
            "sprite" or "sp" => RunSpriteCommand(args[1..]),
            "parse" => SpriteCommands.RunParse(args[1..]) == 0,
            "toquad" or "sq" => SpriteCommands.RunToquad(args[1..]) == 0,
            "spine" or "s2" => SpineCommands.RunSpine(args[1..]) == 0,
            "convert" or "c" => ConvertCommands.RunConvert(args[1..]) == 0,
            "help" or "--help" or "-h" => PrintMainHelp(),
            _ => false
        };
    }

    private static bool RunDecode(string[] args)
    {
        return TexCommands.RunDecode(args) == 0;
    }

    private static bool RunTexCommand(string[] args)
    {
        if (args.Length == 0)
        {
            PrintTexHelp();
            return true;
        }

        var subCommand = args[0].ToLowerInvariant();
        return subCommand switch
        {
            "decode" or "td" => TexCommands.RunDecode(args[1..]) == 0,
            "help" or "--help" or "-h" => PrintTexHelp(),
            _ => false
        };
    }

    private static bool RunSpriteCommand(string[] args)
    {
        if (args.Length == 0)
        {
            PrintSpriteHelp();
            return true;
        }

        var subCommand = args[0].ToLowerInvariant();
        return subCommand switch
        {
            "parse" or "sp" => SpriteCommands.RunParse(args[1..]) == 0,
            "toquad" or "sq" => SpriteCommands.RunToquad(args[1..]) == 0,
            "help" or "--help" or "-h" => PrintSpriteHelp(),
            _ => false
        };
    }

    private static bool PrintMainHelp()
    {
        Console.Error.WriteLine("QTSAvalonia - Vanillaware file converter");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: qtstool <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  tex (t)        Texture file operations");
        Console.Error.WriteLine("  sprite (sp)    Sprite file operations");
        Console.Error.WriteLine("  spine (s2)     Convert QUAD to Spine 2D");
        Console.Error.WriteLine("  convert (c)    One-click conversion");
        Console.Error.WriteLine("  help           Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Run 'qtstool <command> --help' for more information on a command.");
        return true;
    }

    private static bool PrintTexHelp()
    {
        Console.Error.WriteLine("Usage: qtstool tex <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  decode (td)    Decode FTEX/FTP texture files to images");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options for decode:");
        Console.Error.WriteLine("  --input, -i    Input file or directory (required)");
        Console.Error.WriteLine("  --output, -o   Output directory (default: same as input)");
        Console.Error.WriteLine("  --format, -f   Output format: png or clut (default: png)");
        Console.Error.WriteLine("  --verbose, -v  Enable verbose logging");
        return true;
    }

    private static bool PrintSpriteHelp()
    {
        Console.Error.WriteLine("Usage: qtstool sprite <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  parse (sp)     Parse MBP/MBS to V55 JSON");
        Console.Error.WriteLine("  toquad (sq)    Convert to QUAD format");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Run 'qtstool sprite <command> --help' for more information.");
        return true;
    }

    private static bool IsKnownCommand(string command)
    {
        return KnownCommands.Contains(command);
    }

    public static void StartGui(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    internal static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
