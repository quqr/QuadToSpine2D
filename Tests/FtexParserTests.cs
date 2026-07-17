using System.Collections.Concurrent;
using VanillawareConverter.Ftex;
using VanillawareConverter.Ftex.Parsers;

namespace Tests;

/// <summary>
///     FTEX 解析器集成测试，使用 Parallel.ForEach 并行处理所有文件。
/// </summary>
public class FtexParserTests
{
    private const string TestDataRoot = @"F:\Codes\13\13rim ex";
    private static readonly SwitchFtexParser _parser = new();

    /// <summary>并行处理选项：使用所有可用 CPU 核心</summary>
    private static readonly ParallelOptions ParallelOpts = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    /// <summary>获取所有 .ftx 文件路径</summary>
    private static string[] FtxFiles =>
        Directory.Exists(TestDataRoot)
            ? Directory.GetFiles(TestDataRoot, "*.ftx", SearchOption.AllDirectories)
            : [];

    #region 并行集成测试

    [Fact]
    public void Parse_AllFtxFiles_Parallel()
    {
        var files = FtxFiles;
        if (files.Length == 0) return;

        var errors = new ConcurrentBag<string>();
        var processedCount = 0;

        Parallel.ForEach(files, ParallelOpts, ftxPath =>
        {
            if (!File.Exists(ftxPath)) return;

            var name = Path.GetFileNameWithoutExtension(ftxPath);
            try
            {
                var data = File.ReadAllBytes(ftxPath);

                if (!_parser.CanParse(data))
                {
                    errors.Add($"{name}: CanParse failed");
                    return;
                }

                var results = _parser.Parse(data, name);
                if (results.Count == 0)
                {
                    errors.Add($"{name}: No images returned");
                    return;
                }

                foreach (var img in results)
                {
                    if (img.Width <= 0)
                        errors.Add($"{name}: Width must be positive, got {img.Width}");
                    if (img.Height <= 0)
                        errors.Add($"{name}: Height must be positive, got {img.Height}");
                    if (img.PixelData.Length == 0)
                        errors.Add($"{name}: PixelData is empty");

                    var expectedSize = img.Palette != null && img.ColorCount > 0
                        ? img.Width * img.Height
                        : img.Width * img.Height * 4;
                    if (img.PixelData.Length < expectedSize)
                        errors.Add($"{name}: PixelData length {img.PixelData.Length} < expected {expectedSize}");

                    if (img.Palette != null && img.ColorCount <= 0)
                        errors.Add($"{name}: ColorCount should be positive for paletted image");
                }

                Interlocked.Increment(ref processedCount);
            }
            catch (Exception ex)
            {
                errors.Add($"{name}: Exception - {ex.Message}");
            }
        });

        // 输出处理统计
        Console.WriteLine(
            $"Processed {processedCount}/{files.Length} FTX files with {Environment.ProcessorCount} CPU cores");

        // 所有错误一起报告
        if (!errors.IsEmpty)
            Assert.Fail($"FTEX parsing errors:\n{string.Join("\n", errors.Take(20))}" +
                        (errors.Count > 20 ? $"\n... and {errors.Count - 20} more errors" : ""));
    }

    #endregion

    #region 单元测试（无需文件）

    [Fact]
    public void SwitchFtexParser_Platform_IsSwitch()
    {
        Assert.Equal(GamePlatform.Switch, _parser.Platform);
    }

    [Fact]
    public void CanParse_NullData_ReturnsFalse()
    {
        Assert.False(_parser.CanParse(null!));
    }

    [Fact]
    public void CanParse_EmptyData_ReturnsFalse()
    {
        Assert.False(_parser.CanParse([]));
    }

    [Fact]
    public void CanParse_InvalidMagic_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        Assert.False(_parser.CanParse(data));
    }

    [Fact]
    public void CanParse_ValidMagic_ReturnsTrue()
    {
        var data = "FTEX"u8.ToArray();
        Assert.True(_parser.CanParse(data));
    }

    [Fact]
    public void Parse_InvalidData_ReturnsEmptyList()
    {
        var data = new byte[100];
        var result = _parser.Parse(data, "test");
        Assert.Empty(result);
    }

    [Fact]
    public void CanParse_TooShortData_ReturnsFalse()
    {
        var data = new byte[] { 0x46, 0x54, 0x45 }; // "FTE"
        Assert.False(_parser.CanParse(data));
    }

    [Fact]
    public void Parse_CannotParse_ReturnsEmptyList()
    {
        var result = _parser.Parse(new byte[10], "test");
        Assert.Empty(result);
    }

    #endregion
}