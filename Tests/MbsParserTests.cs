using System.Collections.Concurrent;
using VanillawareConverter.Mbs.Models;
using VanillawareConverter.Mbs.Parsers;

namespace Tests;

/// <summary>
///     MBS 解析器集成测试，使用 Parallel.ForEach 并行处理所有文件。
/// </summary>
public class MbsParserTests
{
    private const string TestDataRoot = @"F:\Codes\13\13rim ex";
    private static readonly MbsToV55Parser _parser = new();

    /// <summary>并行处理选项：使用所有可用 CPU 核心</summary>
    private static readonly ParallelOptions ParallelOpts = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    /// <summary>获取所有 .mbs 文件路径</summary>
    private static string[] MbsFiles =>
        Directory.Exists(TestDataRoot)
            ? Directory.GetFiles(TestDataRoot, "*.mbs", SearchOption.AllDirectories)
            : [];

    #region 单元测试（无需文件）

    [Fact]
    public void MbsToV55Parser_UnknownPlatform_Throws()
    {
        var data = new byte[100];
        Assert.Throws<NotSupportedException>(() => _parser.Parse(data, PlatformTag.Unknown));
    }

    #endregion

    #region 并行集成测试

    [Fact]
    public void Parse_AllMbsFiles_Parallel()
    {
        var files = MbsFiles;
        if (files.Length == 0) return;

        var errors = new ConcurrentBag<string>();
        var processedCount = 0;

        Parallel.ForEach(files, ParallelOpts, mbsPath =>
        {
            if (!File.Exists(mbsPath)) return;

            var name = Path.GetFileNameWithoutExtension(mbsPath);
            try
            {
                var data = File.ReadAllBytes(mbsPath);
                var tag = PlatformConfigs.DetectPlatform(data);
                if (tag == PlatformTag.Unknown)
                {
                    errors.Add($"{name}: Unknown platform");
                    return;
                }

                var result = _parser.Parse(data, tag);

                if (result == null)
                {
                    errors.Add($"{name}: Result is null");
                    return;
                }

                var expectedTag = PlatformConfigs.GetTagString(tag);
                if (result.Tag != expectedTag)
                    errors.Add($"{name}: Tag should be '{expectedTag}', got '{result.Tag}'");

                if (result.Ver != "55")
                    errors.Add($"{name}: Ver should be '55', got '{result.Ver}'");

                if (result.S0 == null)
                    errors.Add($"{name}: S0 is null");

                if (result.S1 == null)
                    errors.Add($"{name}: S1 is null");

                if (result.S4 == null)
                    errors.Add($"{name}: S4 is null");

                if (result.S9 == null)
                    errors.Add($"{name}: S9 is null");

                // 验证纹理字段
                if (result.S4 != null)
                    foreach (var tex in result.S4.Where(t => t != null))
                    {
                        if (string.IsNullOrEmpty(tex.I))
                            errors.Add($"{name}: Texture identifier is empty");

                        if (tex.S0S1S2 == null || tex.S0S1S2.Length != 3)
                        {
                            errors.Add($"{name}: Texture S0S1S2 should have 3 elements");
                        }
                        else
                        {
                            if (tex.S0S1S2[0] < 0)
                                errors.Add($"{name}: S0 index negative");
                            if (tex.S0S1S2[1] < 0)
                                errors.Add($"{name}: S1 index negative");
                            if (tex.S0S1S2[2] < 0)
                                errors.Add($"{name}: S2 index negative");
                        }
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
            $"Processed {processedCount}/{files.Length} MBS files with {Environment.ProcessorCount} CPU cores");

        // 所有错误一起报告
        if (!errors.IsEmpty)
            Assert.Fail($"MBS parsing errors:\n{string.Join("\n", errors.Take(20))}" +
                        (errors.Count > 20 ? $"\n... and {errors.Count - 20} more errors" : ""));
    }

    #endregion
}