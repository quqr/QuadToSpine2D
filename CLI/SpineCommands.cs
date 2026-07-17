using System.Diagnostics;
using QTSCore.Process;

namespace QTSAvalonia.CLI;

public static class SpineCommands
{
    public static int RunSpine(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintSpineHelp();
            return 0;
        }

        var input = GetOption(args, "--input", "-i");
        var output = GetOption(args, "--output", "-o");
        var textures = GetOption(args, "--textures");
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

        var ext = Path.GetExtension(inputFile.FullName).ToLowerInvariant();
        if (ext is not ".json")
        {
            CliLogger.Error($"Spine command requires QUAD JSON input file, got: {ext}");
            return 1;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            CliLogger.Start("spine", input);

            Instances.Initialize();

            var quadData = new ProcessQuadJsonFile().LoadQuadJson(inputFile.FullName, true);

            var spineJson = new ProcessSpine2DJson(quadData);
            var spineData = spineJson.Process();

            var outputPath = string.IsNullOrEmpty(output)
                ? Path.Combine(inputFile.DirectoryName!, Path.GetFileNameWithoutExtension(input) + "_spine.json")
                : output;

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var json = JsonConvert.SerializeObject(spineData, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            });
            File.WriteAllText(outputPath, json);

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

    private static void PrintSpineHelp()
    {
        Console.Error.WriteLine("Usage: qtstool spine [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Convert QUAD JSON to Spine 2D format");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --input, -i      Input QUAD JSON file (required)");
        Console.Error.WriteLine("  --output, -o     Output Spine JSON file (default: <input>_spine.json)");
        Console.Error.WriteLine("  --textures       Texture directory for images");
        Console.Error.WriteLine("  --verbose, -v    Enable verbose logging");
        Console.Error.WriteLine("  --help, -h       Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Example:");
        Console.Error.WriteLine("  qtstool spine -i sprite.quad.json");
        Console.Error.WriteLine("  qtstool spine -i sprite.quad.json -o output/Result.json");
    }
}