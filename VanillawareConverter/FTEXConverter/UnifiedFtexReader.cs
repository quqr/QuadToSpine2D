using QTSAvalonia.Helper;
using VanillawareConverter.Ftex.Parsers;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex;

/// <summary>
///     统一FTEX文件读取器
/// </summary>
/// <remarks>
///     提供跨平台的FTEX纹理文件解析功能，支持自动检测平台类型
/// </remarks>
public class UnifiedFtexReader
{
    private readonly List<IFtexParser> _parsers =
    [
        new Ps2FtexParser(),
        new Ps3FtexParser(),
        new Ps4FtexParser(),
        new PspFtexParser(),
        new PsvitaFtexParser(),
        new NdsFtexParser(),
        new WiiFtexParser(),
        new SwitchFtexParser()
    ];

    /// <summary>
    ///     检测文件所属的游戏平台
    /// </summary>
    /// <param name="fileData">文件字节数据</param>
    /// <returns>检测到的平台类型</returns>
    public GamePlatform DetectPlatform(byte[] fileData)
    {
        foreach (var parser in _parsers)
            if (parser.CanParse(fileData))
                return parser.Platform;

        return GamePlatform.Unknown;
    }

    /// <summary>
    ///     解析FTEX文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>解析后的图像结果列表</returns>
    public List<ImageResult> ParseFile(string filePath)
    {
        var results = new List<ImageResult>();

        if (!File.Exists(filePath))
        {
            LoggerHelper.Error($"[文件错误] 文件不存在 - 路径: {filePath}");
            return results;
        }

        LoggerHelper.Info($"[文件解析] 开始解析 - 路径: {filePath}");

        var fileData = File.ReadAllBytes(filePath);

        if (FcmpDecoder.IsFcmpFile(fileData))
        {
            LoggerHelper.Info("[压缩检测] 检测到FCMP压缩格式，正在解压...");
            fileData = FcmpDecoder.Decode(fileData);
            if (fileData.Length > 0) LoggerHelper.Info($"[压缩检测] 解压成功 - 解压后大小: {fileData.Length}字节");
        }

        if (fileData.Length == 0)
        {
            LoggerHelper.Error($"[文件错误] 文件内容为空或解压失败 - 路径: {filePath}");
            return results;
        }

        var outputPrefix = Path.ChangeExtension(filePath, null);

        foreach (var parser in _parsers)
            if (parser.CanParse(fileData))
            {
                LoggerHelper.Info($"[平台检测] 识别成功 - 平台: {parser.Platform}, 文件: {Path.GetFileName(filePath)}");
                return parser.Parse(fileData, outputPrefix);
            }

        LoggerHelper.Warning($"[平台检测] 无法识别文件格式 - 路径: {filePath}");
        return results;
    }

    /// <summary>
    ///     解析并保存纹理文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="convertToPng">是否转换为PNG格式</param>
    /// <param name="outputDirectory">自定义输出目录（可选，默认输出到输入文件所在目录）</param>
    public void ParseAndSave(string filePath, bool convertToPng = true, string? outputDirectory = null)
    {
        LoggerHelper.Info($"[批量处理] 开始处理文件 - 路径: {filePath}, 转换PNG: {convertToPng}");

        var results = ParseFile(filePath);

        var outputDir = outputDirectory ?? Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var outputPrefix = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(filePath));

        var index = 0;
        foreach (var result in results)
        {
            var baseName = $"{outputPrefix}.{index}";
            var filename = $"{baseName}.nvt";

            NvtFileWriter.Save(filename, result);

            if (convertToPng) PngConverter.ConvertNvtToPng(filename);

            index++;
        }

        LoggerHelper.Info($"[批量处理] 处理完成 - 共提取 {results.Count} 个纹理");
    }

    /// <summary>
    ///     批量处理目录中的FTEX文件
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="pattern">文件匹配模式</param>
    /// <param name="convertToPng">是否转换为PNG格式</param>
    public static void ProcessDirectory(string directoryPath, string pattern = "*.ftx", bool convertToPng = true)
    {
        if (!Directory.Exists(directoryPath))
        {
            LoggerHelper.Error($"[目录处理] 目录不存在 - 路径: {directoryPath}");
            return;
        }

        LoggerHelper.Info($"[目录处理] 开始扫描目录 - 路径: {directoryPath}, 匹配模式: {pattern}");

        var reader = new UnifiedFtexReader();
        var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);

        LoggerHelper.Info($"[目录处理] 找到 {files.Length} 个匹配文件");

        var successCount = 0;
        var failCount = 0;

        Parallel.ForEach(files, file =>
        {
            LoggerHelper.Info(
                $"[目录处理] 正在处理 ({successCount + failCount + 1}/{files.Length}) - 文件: {Path.GetFileName(file)}");
            try
            {
                reader.ParseAndSave(file, convertToPng);
                successCount++;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[目录处理] 处理失败 - 文件: {Path.GetFileName(file)}", ex);
                failCount++;
            }
        });

        LoggerHelper.Info($"[目录处理] 批量处理完成 - 成功: {successCount}, 失败: {failCount}, 总计: {files.Length}");
    }
}