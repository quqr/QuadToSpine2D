using VanillawareConverter.Ftex;
using VanillawareConverter.Mbs.Models;
using VanillawareConverter.Mbs.Parsers;
using VanillawareConverter.Mbs.Converters;
using Newtonsoft.Json;
using QTSCore.Process;

namespace QTSAvalonia.CLI;

public static class ConvertCommands
{
    public static int RunConvert(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintConvertHelp();
            return 0;
        }

        var input = GetOption(args, "--input", "-i");
        var output = GetOption(args, "--output", "-o");
        var game = GetOption(args, "--game", "-g");
        var textures = GetOption(args, "--textures");
        var format = GetOption(args, "--format", "-f") ?? "png";
        var verbose = HasFlag(args, "--verbose", "-v");

        CliLogger.SetVerbose(verbose);

        if (string.IsNullOrEmpty(input))
        {
            CliLogger.Error("Missing required option: --input");
            return 1;
        }

        var inputFile = new FileInfo(input);
        if (!inputFile.Exists && !Directory.Exists(input))
        {
            CliLogger.Error($"File or directory not found: {input}");
            return 1;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            CliLogger.Start("convert", input);

            if (Directory.Exists(input))
                return ConvertDirectory(new DirectoryInfo(input), output, game, textures, format, sw);

            return ConvertFile(inputFile, output, game, textures, format, sw);
        }
        catch (Exception ex)
        {
            sw.Stop();
            CliLogger.Error($"Failed to convert: {ex.Message}");
            return 1;
        }
    }

    private static int ConvertFile(FileInfo inputFile, string? output, string? game, string? textures, string format, System.Diagnostics.Stopwatch sw)
    {
        var ext = Path.GetExtension(inputFile.FullName).ToLowerInvariant();

        switch (ext)
        {
            case ".ftp" or ".ftx":
                return ConvertTexture(inputFile, output, format, sw);
            case ".mbs" or ".mbp":
                return ConvertSprite(inputFile, output, game, textures, sw);
            case ".json":
                return ConvertQuadJson(inputFile, output, sw);
            default:
                CliLogger.Error($"Unknown file type: {ext}. Supported: .ftp, .ftx, .mbs, .mbp, .json");
                return 1;
        }
    }

    private static int ConvertDirectory(DirectoryInfo inputDir, string? output, string? game, string? textures, string format, System.Diagnostics.Stopwatch sw)
    {
        var outputDir = string.IsNullOrEmpty(output)
            ? new DirectoryInfo(Path.Combine(inputDir.FullName, "output"))
            : new DirectoryInfo(output);

        if (!outputDir.Exists)
            outputDir.Create();

        var ftxFiles = Directory.GetFiles(inputDir.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ftp" or ".ftx")
            .ToArray();

        var mbsFiles = Directory.GetFiles(inputDir.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".mbs" or ".mbp")
            .ToArray();

        var successCount = 0;
        var failCount = 0;

        if (ftxFiles.Length > 0)
        {
            CliLogger.Info($"Processing {ftxFiles.Length} texture files...");
            var texOutputDir = Path.Combine(outputDir.FullName, "textures");
            if (!Directory.Exists(texOutputDir))
                Directory.CreateDirectory(texOutputDir);

            var reader = new UnifiedFtexReader();
            for (var i = 0; i < ftxFiles.Length; i++)
            {
                try
                {
                    reader.ParseAndSave(ftxFiles[i], format.ToLowerInvariant() == "png", texOutputDir);
                    CliLogger.Progress(i + 1, ftxFiles.Length, $"Texture: {Path.GetFileName(ftxFiles[i])} ✓");
                    successCount++;
                }
                catch (Exception ex)
                {
                    CliLogger.Progress(i + 1, ftxFiles.Length, $"Texture: {Path.GetFileName(ftxFiles[i])} ✗ {ex.Message}");
                    failCount++;
                }
            }
        }

        if (mbsFiles.Length > 0)
        {
            CliLogger.Info($"Processing {mbsFiles.Length} sprite files...");
            var quadOutputDir = Path.Combine(outputDir.FullName, "quads");
            if (!Directory.Exists(quadOutputDir))
                Directory.CreateDirectory(quadOutputDir);

            for (var i = 0; i < mbsFiles.Length; i++)
            {
                try
                {
                    var fileData = File.ReadAllBytes(mbsFiles[i]);
                    var tag = PlatformConfigs.DetectPlatform(fileData);

                    if (tag == PlatformTag.Unknown && !string.IsNullOrEmpty(game))
                        tag = ParseGameTag(game);

                    if (tag == PlatformTag.Unknown)
                    {
                        CliLogger.Progress(i + 1, mbsFiles.Length, $"Sprite: {Path.GetFileName(mbsFiles[i])} ✗ Unknown platform");
                        failCount++;
                        continue;
                    }

                    var parser = new MbsToV55Parser();
                    var v55Data = parser.Parse(fileData, tag);
                    var converter = new V55ToQuadConverter();
                    var quadData = converter.Convert(v55Data);

                    var quadPath = Path.Combine(quadOutputDir, Path.GetFileNameWithoutExtension(mbsFiles[i]) + ".quad.json");
                    var quadJson = JsonConvert.SerializeObject(quadData, Formatting.Indented);
                    File.WriteAllText(quadPath, quadJson);

                    CliLogger.Progress(i + 1, mbsFiles.Length, $"Sprite: {Path.GetFileName(mbsFiles[i])} ✓");
                    successCount++;
                }
                catch (Exception ex)
                {
                    CliLogger.Progress(i + 1, mbsFiles.Length, $"Sprite: {Path.GetFileName(mbsFiles[i])} ✗ {ex.Message}");
                    failCount++;
                }
            }
        }

        sw.Stop();
        CliLogger.Done(successCount + failCount, sw.Elapsed);
        return failCount > 0 ? 1 : 0;
    }

    private static int ConvertTexture(FileInfo inputFile, string? output, string format, System.Diagnostics.Stopwatch sw)
    {
        var outputDir = string.IsNullOrEmpty(output)
            ? inputFile.DirectoryName
            : Path.GetDirectoryName(output);

        var reader = new UnifiedFtexReader();
        reader.ParseAndSave(inputFile.FullName, format.ToLowerInvariant() == "png", outputDir);

        sw.Stop();
        CliLogger.Done(1, sw.Elapsed);
        return 0;
    }

    private static int ConvertSprite(FileInfo inputFile, string? output, string? game, string? textures, System.Diagnostics.Stopwatch sw)
    {
        var fileData = File.ReadAllBytes(inputFile.FullName);
        var tag = PlatformConfigs.DetectPlatform(fileData);

        if (tag == PlatformTag.Unknown && !string.IsNullOrEmpty(game))
            tag = ParseGameTag(game);

        if (tag == PlatformTag.Unknown)
        {
            CliLogger.Error("Cannot auto-detect platform. Please specify --game");
            return 1;
        }

        CliLogger.Info($"Platform: {PlatformConfigs.GetTagString(tag)}");

        var parser = new MbsToV55Parser();
        var v55Data = parser.Parse(fileData, tag);
        var converter = new V55ToQuadConverter();
        var quadData = converter.Convert(v55Data);

        var outputPath = string.IsNullOrEmpty(output)
            ? Path.ChangeExtension(inputFile.FullName, ".quad.json")
            : output;

        var quadJson = JsonConvert.SerializeObject(quadData, Formatting.Indented);
        File.WriteAllText(outputPath, quadJson);

        sw.Stop();
        CliLogger.Done(1, sw.Elapsed);
        CliLogger.Info($"Output: {outputPath}");
        return 0;
    }

    private static int ConvertQuadJson(FileInfo inputFile, string? output, System.Diagnostics.Stopwatch sw)
    {
        Instances.Initialize();

        var quadData = new ProcessQuadJsonFile().LoadQuadJson(inputFile.FullName, true);

        var spineJson = new ProcessSpine2DJson(quadData);
        var spineData = spineJson.Process();

        var outputPath = string.IsNullOrEmpty(output)
            ? Path.Combine(inputFile.DirectoryName!, "Result.json")
            : output;

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var json = JsonConvert.SerializeObject(spineData, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        });
        File.WriteAllText(outputPath, json);

        sw.Stop();
        CliLogger.Done(1, sw.Elapsed);
        CliLogger.Info($"Output: {outputPath}");
        return 0;
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
        {
            if (names.Contains(args[i]))
                return args[i + 1];
        }
        return null;
    }

    private static bool HasFlag(string[] args, params string[] names)
    {
        return args.Any(a => names.Contains(a));
    }

    private static void PrintConvertHelp()
    {
        Console.Error.WriteLine("Usage: qtstool convert [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("One-click conversion with auto-detection");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --input, -i      Input file or directory (required)");
        Console.Error.WriteLine("  --output, -o     Output directory (default: same as input)");
        Console.Error.WriteLine("  --game, -g       Game ID for sprite files");
        Console.Error.WriteLine("  --textures       Texture directory for sprites");
        Console.Error.WriteLine("  --format, -f     Texture format: png or clut (default: png)");
        Console.Error.WriteLine("  --verbose, -v    Enable verbose logging");
        Console.Error.WriteLine("  --help, -h       Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Supported input types:");
        Console.Error.WriteLine("  .ftp, .ftx   Texture files → PNG/CLUT images");
        Console.Error.WriteLine("  .mbs, .mbp   Sprite files → QUAD JSON");
        Console.Error.WriteLine("  .json        QUAD JSON → Spine 2D JSON");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  qtstool convert -i textures/");
        Console.Error.WriteLine("  qtstool convert -i sprite.mbs -g ps4_sent");
        Console.Error.WriteLine("  qtstool convert -i sprite.quad.json -o spine/");
    }
}
