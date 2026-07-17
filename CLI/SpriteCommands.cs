using System.Diagnostics;
using VanillawareConverter.Mbs.Converters;
using VanillawareConverter.Mbs.Models;
using VanillawareConverter.Mbs.Parsers;

namespace QTSAvalonia.CLI;

public static class SpriteCommands
{
    public static int RunParse(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintParseHelp();
            return 0;
        }

        var input = GetOption(args, "--input", "-i");
        var output = GetOption(args, "--output", "-o");
        var game = GetOption(args, "--game", "-g");
        var verbose = HasFlag(args, "--verbose", "-v");

        CliLogger.SetVerbose(verbose);

        if (string.IsNullOrEmpty(input))
        {
            CliLogger.Error("Missing required option: --input");
            return 1;
        }

        var inputFile = new FileInfo(input);
        if (!inputFile.Exists)
        {
            CliLogger.Error($"File not found: {input}");
            return 1;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var fileData = File.ReadAllBytes(inputFile.FullName);
            var tag = PlatformConfigs.DetectPlatform(fileData);

            if (tag == PlatformTag.Unknown && string.IsNullOrEmpty(game))
            {
                CliLogger.Error("Cannot auto-detect platform. Please specify --game");
                return 1;
            }

            if (!string.IsNullOrEmpty(game))
            {
                tag = ParseGameTag(game);
                if (tag == PlatformTag.Unknown)
                {
                    CliLogger.Error($"Unknown game tag: {game}");
                    return 1;
                }
            }

            CliLogger.Info($"Platform: {PlatformConfigs.GetTagString(tag)}");

            var parser = new MbsToV55Parser();
            var v55Data = parser.Parse(fileData, tag);

            var outputPath = string.IsNullOrEmpty(output)
                ? Path.ChangeExtension(input, ".v55.json")
                : output;

            CliLogger.Start("sprite parse", input);

            var json = JsonConvert.SerializeObject(v55Data, Formatting.Indented);
            File.WriteAllText(outputPath, json);

            sw.Stop();
            CliLogger.Done(1, sw.Elapsed);
            CliLogger.Info($"Output: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            CliLogger.Error($"Failed to parse {Path.GetFileName(input)}: {ex.Message}");
            return 1;
        }
    }

    public static int RunToquad(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintToquadHelp();
            return 0;
        }

        var input = GetOption(args, "--input", "-i");
        var output = GetOption(args, "--output", "-o");
        var game = GetOption(args, "--game", "-g");
        var verbose = HasFlag(args, "--verbose", "-v");

        CliLogger.SetVerbose(verbose);

        if (string.IsNullOrEmpty(input))
        {
            CliLogger.Error("Missing required option: --input");
            return 1;
        }

        var inputFile = new FileInfo(input);
        if (!inputFile.Exists)
        {
            CliLogger.Error($"File not found: {input}");
            return 1;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var ext = Path.GetExtension(input).ToLowerInvariant();
            V55Data v55Data;

            if (ext is ".mbs" or ".mbp")
            {
                var fileData = File.ReadAllBytes(inputFile.FullName);
                var tag = PlatformConfigs.DetectPlatform(fileData);

                if (tag == PlatformTag.Unknown && string.IsNullOrEmpty(game))
                {
                    CliLogger.Error("Cannot auto-detect platform. Please specify --game");
                    return 1;
                }

                if (!string.IsNullOrEmpty(game))
                {
                    tag = ParseGameTag(game);
                    if (tag == PlatformTag.Unknown)
                    {
                        CliLogger.Error($"Unknown game tag: {game}");
                        return 1;
                    }
                }

                CliLogger.Info($"Platform: {PlatformConfigs.GetTagString(tag)}");

                var parser = new MbsToV55Parser();
                v55Data = parser.Parse(fileData, tag);
            }
            else if (ext is ".json" or ".v55")
            {
                var json = File.ReadAllText(inputFile.FullName);
                v55Data = JsonConvert.DeserializeObject<V55Data>(json)
                          ?? throw new ArgumentException("Invalid V55 JSON file");
            }
            else
            {
                CliLogger.Error($"Unsupported file type: {ext}. Use .MBP/.MBS or .V55.json");
                return 1;
            }

            var converter = new V55ToQuadConverter();
            var quadData = converter.Convert(v55Data);

            var outputPath = string.IsNullOrEmpty(output)
                ? Path.ChangeExtension(input, ".quad.json")
                : output;

            CliLogger.Start("sprite toquad", input);

            var quadJson = JsonConvert.SerializeObject(quadData, Formatting.Indented);
            File.WriteAllText(outputPath, quadJson);

            sw.Stop();
            CliLogger.Done(1, sw.Elapsed);
            CliLogger.Info($"Output: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            CliLogger.Error($"Failed to convert {Path.GetFileName(input)}: {ex.Message}");
            return 1;
        }
    }

    private static PlatformTag ParseGameTag(string game)
    {
        return game.ToLowerInvariant() switch
        {
            "ps2_grim" => PlatformTag.Ps2Grim,
            "ps2_odin" => PlatformTag.Ps2Odin,
            "nds_kuma" => PlatformTag.NdsKuma,
            "wii_mura" => PlatformTag.WiiMura,
            "ps3_drag" => PlatformTag.Ps3Drag,
            "ps3_odin" => PlatformTag.Ps3Odin,
            "ps4_odin" => PlatformTag.Ps4Odin,
            "ps4_drag" => PlatformTag.Ps4Drag,
            "ps4_sent" => PlatformTag.Ps4Sent,
            "swi_sent" => PlatformTag.SwiSent,
            "swi_grim" => PlatformTag.SwiGrim,
            "swi_unic" => PlatformTag.SwiUnic,
            "ps4_unic" => PlatformTag.Ps4Unic,
            _ => PlatformTag.Unknown
        };
    }

    private static string? GetOption(string[] args, params string[] names)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (names.Contains(args[i]))
                return args[i + 1];
        return null;
    }

    private static bool HasFlag(string[] args, params string[] names)
    {
        return args.Any(a => names.Contains(a));
    }

    private static void PrintParseHelp()
    {
        Console.Error.WriteLine("Usage: qtstool sprite parse [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Parse MBP/MBS sprite data to V55 JSON format");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --input, -i    Input MBP/MBS file (required)");
        Console.Error.WriteLine("  --output, -o   Output V55 JSON file (default: <input>.v55.json)");
        Console.Error.WriteLine("  --game, -g     Game ID (e.g., ps2_grim, swi_unic)");
        Console.Error.WriteLine("  --verbose, -v  Enable verbose logging");
        Console.Error.WriteLine("  --help, -h     Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Game IDs:");
        Console.Error.WriteLine("  ps2_grim  PS2   GrimGrimoire");
        Console.Error.WriteLine("  ps2_odin  PS2   Odin Sphere");
        Console.Error.WriteLine("  nds_kuma  NDS   Kumatanchi");
        Console.Error.WriteLine("  wii_mura  Wii   Muramasa");
        Console.Error.WriteLine("  ps3_drag  PS3   Dragon's Crown");
        Console.Error.WriteLine("  ps3_odin  PS3   Odin Sphere Leifthrasir");
        Console.Error.WriteLine("  ps4_sent  PS4   13 Sentinels");
        Console.Error.WriteLine("  swi_sent  Swit  13 Sentinels");
        Console.Error.WriteLine("  swi_grim  Swit  GrimGrimoire HD");
        Console.Error.WriteLine("  swi_unic  Swit  Unicorn Overlord");
        Console.Error.WriteLine("  ps4_unic  PS4   Unicorn Overlord");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  qtstool sprite parse -i sprite.mbs");
        Console.Error.WriteLine("  qtstool sprite parse -i sprite.mbs -o output.v55.json -g ps4_sent");
    }

    private static void PrintToquadHelp()
    {
        Console.Error.WriteLine("Usage: qtstool sprite toquad [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Convert MBP/MBS sprite data to QUAD format");
        Console.Error.WriteLine("Pipeline: MBP/MBS → V55 → QUAD");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --input, -i    Input MBP/MBS or V55 JSON file (required)");
        Console.Error.WriteLine("  --output, -o   Output QUAD JSON file (default: <input>.quad.json)");
        Console.Error.WriteLine("  --game, -g     Game ID (required if input is MBP/MBS)");
        Console.Error.WriteLine("  --verbose, -v  Enable verbose logging");
        Console.Error.WriteLine("  --help, -h     Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  qtstool sprite toquad -i sprite.mbs");
        Console.Error.WriteLine("  qtstool sprite toquad -i sprite.mbs -g ps4_sent");
    }
}