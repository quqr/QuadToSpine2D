using QTSAvalonia.Helper;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex;

/// <summary>
/// NVT格式文件写入器
/// </summary>
/// <remarks>
/// 提供统一的NVT纹理文件保存功能，支持CLUT和RGBA两种格式
/// </remarks>
public static class NvtFileWriter
{
    /// <summary>
    /// 保存NVT格式文件（自动选择CLUT或RGBA格式）
    /// </summary>
    /// <param name="filename">目标文件名</param>
    /// <param name="data">图像数据</param>
    public static void Save(string filename, ImageResult data)
    {
        if (data.Palette != null)
        {
            SaveClut(filename, data);
        }
        else
        {
            SaveRgba(filename, data);
        }
    }

    /// <summary>
    /// 保存CLUT格式文件
    /// </summary>
    /// <param name="filename">目标文件名</param>
    /// <param name="data">图像数据</param>
    public static void SaveClut(string filename, ImageResult data)
    {
        try
        {
            EnsureDirectoryExists(filename);

            using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            bw.Write("CLUT"u8.ToArray());
            bw.Write(data.ColorCount);
            bw.Write(data.Width);
            bw.Write(data.Height);
            bw.Write(data.Palette ?? Array.Empty<byte>());
            bw.Write(data.PixelData);

            LoggerHelper.Info($"[文件保存] CLUT文件保存成功 - 文件名: {filename}, 尺寸: {data.Width}x{data.Height}, 颜色数: {data.ColorCount}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"[文件保存] CLUT文件保存失败 - 文件名: {filename}", ex);
        }
    }

    /// <summary>
    /// 保存RGBA格式文件
    /// </summary>
    /// <param name="filename">目标文件名</param>
    /// <param name="data">图像数据</param>
    public static void SaveRgba(string filename, ImageResult data)
    {
        try
        {
            EnsureDirectoryExists(filename);

            using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            bw.Write("RGBA"u8.ToArray());
            bw.Write(data.Width);
            bw.Write(data.Height);
            bw.Write(data.PixelData);

            LoggerHelper.Info($"[文件保存] RGBA文件保存成功 - 文件名: {filename}, 尺寸: {data.Width}x{data.Height}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"[文件保存] RGBA文件保存失败 - 文件名: {filename}", ex);
        }
    }

    /// <summary>
    /// 确保目标目录存在
    /// </summary>
    /// <param name="filename">文件名</param>
    private static void EnsureDirectoryExists(string filename)
    {
        var dir = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            LoggerHelper.Debug($"[文件保存] 创建目录 - 路径: {dir}");
        }
    }
}