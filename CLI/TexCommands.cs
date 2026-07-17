using System.Diagnostics;
using VanillawareConverter.Ftex;

namespace QTSAvalonia.CLI;

public static class TexCommands
{
    public static int RunDecode(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintDecodeHelp();
            return 0;
        }

        var input = GetOption(args, "--input", "-i");
        var output = GetOption(args, "--output", "-o");
        var format = GetOption(args, "--format", "-f") ?? "png";
        var verbose = HasFlag(args, "--verbose", "-v");

        CliLogger.SetVerbose(verbose);

        if (string.IsNullOrEmpty(input))
        {
            CliLogger.Error("Missing required option: --input");
            return 1;
        }

        var inputFile = new FileInfo(input);
        if (!inputFile.Exists && !inputFile.Attributes.HasFlag(FileAttributes.Directory))
        {
            CliLogger.Error($"File not found: {input}");
            return 1;
        }

        bool convertToPng;
        switch (format.ToLowerInvariant())
        {
            case "png":
                convertToPng = true;
                break;
            case "clut":
                convertToPng = false;
                break;
            default:
                CliLogger.Error($"Invalid format: {format}. Use 'png' or 'clut'.");
                return 1;
        }

        var outputDir = string.IsNullOrEmpty(output)
            ? new DirectoryInfo(inputFile.DirectoryName!)
            : new DirectoryInfo(output);

        if (!outputDir.Exists)
            outputDir.Create();

        CliLogger.Start("tex decode", input);

        var reader = new UnifiedFtexReader();

        if (inputFile.Attributes.HasFlag(FileAttributes.Directory))
            return ProcessDirectory(reader, inputFile.FullName, outputDir.FullName, convertToPng);

        return ProcessFile(reader, inputFile.FullName, outputDir.FullName, convertToPng);
    }

    private static int ProcessDirectory(UnifiedFtexReader reader, string inputDir, string outputDir, bool convertToPng)
    {
        var files = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".ftp" or ".ftx";
            })
            .ToArray();

        if (files.Length == 0)
        {
            CliLogger.Warning("No .FTP/.FTX files found in directory");
            return 0;
        }

        var sw = Stopwatch.StartNew();
        var successCount = 0;
        var failCount = 0;

        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var relPath = Path.GetRelativePath(inputDir, file);
            var fileOutputDir = Path.Combine(outputDir, Path.GetDirectoryName(relPath) ?? "");

            try
            {
                var fileOutput = new DirectoryInfo(fileOutputDir);
                if (!fileOutput.Exists)
                    fileOutput.Create();

                reader.ParseAndSave(file, convertToPng, fileOutputDir);
                CliLogger.Progress(i + 1, files.Length, $"{relPath} ✓");
                successCount++;
            }
            catch (Exception ex)
            {
                CliLogger.Progress(i + 1, files.Length, $"{relPath} ✗ {ex.Message}");
                failCount++;
            }
        }

        sw.Stop();
        CliLogger.Done(successCount + failCount, sw.Elapsed);
        return failCount > 0 ? 1 : 0;
    }

    private static int ProcessFile(UnifiedFtexReader reader, string filePath, string outputDir, bool convertToPng)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var fileOutput = new DirectoryInfo(outputDir);
            if (!fileOutput.Exists)
                fileOutput.Create();

            reader.ParseAndSave(filePath, convertToPng, outputDir);
            sw.Stop();
            CliLogger.Done(1, sw.Elapsed);
            return 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            CliLogger.Error($"Failed to process {Path.GetFileName(filePath)}: {ex.Message}");
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

    private static void PrintDecodeHelp()
    {
        Console.Error.WriteLine("Usage: qtstool tex decode [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Decode FTEX/FTP texture files to images");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --input, -i    Input file or directory (required)");
        Console.Error.WriteLine("  --output, -o   Output directory (default: same as input)");
        Console.Error.WriteLine("  --format, -f   Output format: png or clut (default: png)");
        Console.Error.WriteLine("  --verbose, -v  Enable verbose logging");
        Console.Error.WriteLine("  --help, -h     Show this help message");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  qtstool tex decode -i textures/");
        Console.Error.WriteLine("  qtstool tex decode -i sprite.ftx -o output/");
    }
}