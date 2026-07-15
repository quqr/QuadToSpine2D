using VanillawareConverter.Ftex;
using VanillawareConverter.Mbs.Models;
using VanillawareConverter.Mbs.Parsers;

namespace QTSCore.Services;

/// <summary>
/// Quad 文件加载服务：封装 FTX/MBS 文件的扫描与解析逻辑，与 UI 状态解耦。
/// </summary>
public class QuadFileLoader
{
    /// <summary>
    /// 递归扫描目录下的受支持文件（.ftx/.ftp 与 .mbs/.mbp），FTX 在前、MBS 在后。
    /// </summary>
    public List<string> ScanSupportedFiles(string directoryPath)
    {
        var ftxFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ftx" or ".ftp")
            .ToList();
        var mbsFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".mbs" or ".mbp")
            .ToList();
        return ftxFiles.Concat(mbsFiles).ToList();
    }

    /// <summary>
    /// 从一组混合路径（目录或文件）中收集受支持的文件。
    /// </summary>
    public List<string> CollectSupportedFiles(IEnumerable<string> paths)
    {
        var filePaths = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                filePaths.AddRange(ScanSupportedFiles(path));
            }
            else if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".ftx" or ".ftp" or ".mbs" or ".mbp")
                    filePaths.Add(path);
            }
        }
        return filePaths;
    }

    /// <summary>
    /// 解析 FTX 文件，返回其中的全部图像结果。
    /// </summary>
    public List<ImageResult> ParseFtx(string ftxPath)
    {
        var reader = new UnifiedFtexReader();
        return reader.ParseFile(ftxPath);
    }

    /// <summary>
    /// 解析 MBS 文件，返回动画数量与骨架名称；平台未知时返回 null。
    /// </summary>
    public MbsParseResult? ParseMbs(string mbsPath)
    {
        var fileData = File.ReadAllBytes(mbsPath);
        var tag = PlatformConfigs.DetectPlatform(fileData);
        if (tag == PlatformTag.Unknown)
            return null;

        var parser = new MbsToV55Parser();
        var v55Data = parser.Parse(fileData, tag);

        // S9 = 骨架数据，过滤空名称
        var skeletonNames = new List<string>();
        for (int i = 0; i < v55Data.S9.Count; i++)
        {
            var bone = v55Data.S9[i];
            if (bone != null && !string.IsNullOrWhiteSpace(bone.Name))
                skeletonNames.Add(bone.Name);
        }

        // 动画数量 = Sa 集合总数
        var animCount = v55Data.Sa.Count;

        return new MbsParseResult(animCount, skeletonNames);
    }
}

/// <summary>
/// MBS 解析结果（动画数量 + 骨架名称）。
/// </summary>
public sealed record MbsParseResult(int AnimationCount, List<string> SkeletonNames);
