using System.Text;
using QTSAvalonia.Helper;
using VanillawareConverter.Ftex.Swizzling;
using VanillawareConverter.Ftex.Textures;
using VanillawareConverter.Common;

namespace VanillawareConverter.Ftex;

/// <summary>
/// 图像解析结果
/// </summary>
public class ImageResult
{
    /// <summary>
    /// 初始化图像结果的新实例
    /// </summary>
    public ImageResult()
    {
    }

    /// <summary>
    /// 使用指定参数初始化图像结果的新实例
    /// </summary>
    /// <param name="width">图像宽度</param>
    /// <param name="height">图像高度</param>
    /// <param name="pixelData">像素数据</param>
    public ImageResult(int width, int height, byte[] pixelData)
    {
        Width = width;
        Height = height;
        PixelData = pixelData;
    }

    /// <summary>
    /// 获取或设置图像宽度（像素）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 获取或设置图像高度（像素）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 获取或设置像素数据
    /// </summary>
    public byte[] PixelData { get; set; } = [];

    /// <summary>
    /// 获取或设置调色板数据
    /// </summary>
    public byte[]? Palette { get; set; }

    /// <summary>
    /// 获取或设置颜色数量
    /// </summary>
    public int ColorCount { get; set; }

    /// <summary>
    /// 获取是否为灰度图像
    /// </summary>
    public bool IsGrayscale => Palette != null;
}

/// <summary>
/// 纹理格式枚举
/// </summary>
public enum TextureFormat
{
    /// <summary>
    /// BC3压缩格式
    /// </summary>
    BC3 = 0x44,

    /// <summary>
    /// BC4压缩格式
    /// </summary>
    BC4 = 0x49,

    /// <summary>
    /// BC7压缩格式
    /// </summary>
    BC7 = 0x4D
}

/// <summary>
/// FTEX纹理文件读取器
/// </summary>
/// <remarks>
/// 用于解析Nintendo Switch平台的FTEX纹理文件格式
/// </remarks>
public class FtexReader
{
    private readonly BptcTexture _bptc = new();
    private readonly S3tcTexture _s3tc = new();

    /// <summary>
    /// 解析BC3格式纹理
    /// </summary>
    private ImageResult ImBc3(byte[] file, int pos, int w, int h, int size)
    {
        LoggerHelper.Debug($"[BC3解码] 开始解码 - 位置: 0x{pos:X}, 尺寸: {w}x{h}, 数据大小: {size}字节");
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _s3tc.Bc3(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle16Bits(pix, w, ch);

        LoggerHelper.Debug($"[BC3解码] 解码完成 - 输出尺寸: {w}x{h}");
        return new ImageResult(w, h, pix);
    }

    /// <summary>
    /// 解析BC4格式纹理
    /// </summary>
    private ImageResult ImBc4(byte[] file, int pos, int w, int h, int size)
    {
        LoggerHelper.Debug($"[BC4解码] 开始解码 - 位置: 0x{pos:X}, 尺寸: {w}x{h}, 数据大小: {size}字节");
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _s3tc.Bc4(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle8Bits(pix, w, ch);

        var result = new ImageResult(w, h, pix)
        {
            ColorCount = 0x100,
            Palette = ByteHelper.GrayClut(0x100)
        };
        LoggerHelper.Debug($"[BC4解码] 解码完成 - 输出尺寸: {w}x{h}, 灰度调色板: 256色");
        return result;
    }

    /// <summary>
    /// 解析BC7格式纹理
    /// </summary>
    private ImageResult ImBc7(byte[] file, int pos, int w, int h, int size)
    {
        LoggerHelper.Debug($"[BC7解码] 开始解码 - 位置: 0x{pos:X}, 尺寸: {w}x{h}, 数据大小: {size}字节");
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _bptc.Bc7(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle16Bits(pix, w, ch);

        LoggerHelper.Debug($"[BC7解码] 解码完成 - 输出尺寸: {w}x{h}");
        return new ImageResult(w, h, pix);
    }

    /// <summary>
    /// 处理Switch NVT纹理块
    /// </summary>
    private void Switnvt(byte[] file, int baseOffset, string prefix, int id)
    {
        LoggerHelper.Debug($"[NVT解析] 开始处理 - 偏移: 0x{baseOffset:X}, 前缀: {prefix}, ID: {id}");

        if (baseOffset + 4 > file.Length)
        {
            LoggerHelper.Warning($"[NVT解析] 偏移超出文件范围 - 偏移: 0x{baseOffset:X}");
            return;
        }

        var magic = Encoding.ASCII.GetString(file, baseOffset, 4);
        if (magic != ".tex")
        {
            LoggerHelper.Warning($"[NVT解析] 无效的纹理魔数 - 期望: .tex, 实际: {magic}");
            return;
        }

        if (baseOffset + 36 > file.Length)
        {
            LoggerHelper.Warning($"[NVT解析] 文件头不完整 - 偏移: 0x{baseOffset:X}");
            return;
        }

        var fmt = (TextureFormat)ByteHelper.ReadUInt16(file, baseOffset + 4);
        var w = (int)ByteHelper.ReadUInt32(file, baseOffset + 12);
        var h = (int)ByteHelper.ReadUInt32(file, baseOffset + 16);
        var sz1 = (int)ByteHelper.ReadUInt32(file, baseOffset + 28);
        var sz2 = (int)ByteHelper.ReadUInt32(file, baseOffset + 32);

        ImageResult? img;
        switch (fmt)
        {
            case TextureFormat.BC3:
                LoggerHelper.Info($"[纹理检测] 格式: BC3 (DXT5), 尺寸: {w}x{h}像素");
                img = ImBc3(file, baseOffset + sz1, w, h, sz2);
                break;
            case TextureFormat.BC4:
                LoggerHelper.Info($"[纹理检测] 格式: BC4 (灰度), 尺寸: {w}x{h}像素");
                img = ImBc4(file, baseOffset + sz1, w, h, sz2);
                break;
            case TextureFormat.BC7:
                LoggerHelper.Info($"[纹理检测] 格式: BC7 (高质量), 尺寸: {w}x{h}像素");
                img = ImBc7(file, baseOffset + sz1, w, h, sz2);
                break;
            default:
                LoggerHelper.Warning($"[纹理检测] 未知的纹理格式 - 格式代码: 0x{fmt:X}");
                return;
        }

        if (img != null)
        {
            var fn = $"{prefix}.{id}.nvt";
            LoggerHelper.Info($"[文件保存] 准备保存纹理 - 文件名: {fn}, 尺寸: {w}x{h}");
            NvtFileWriter.Save(fn, img);
        }
    }

    /// <summary>
    /// 解析FTEX文件
    /// </summary>
    /// <param name="filePath">FTEX文件路径</param>
    /// <returns>解析后的图像结果列表</returns>
    public List<ImageResult> ParseFtex(string filePath)
    {
        var results = new List<ImageResult>();

        if (!File.Exists(filePath))
        {
            LoggerHelper.Error($"[文件错误] 文件不存在 - 路径: {filePath}");
            return results;
        }

        LoggerHelper.Info($"[FTEX解析] 开始解析文件 - 路径: {filePath}");

        var file = File.ReadAllBytes(filePath);
        if (file.Length < 4)
        {
            LoggerHelper.Error($"[文件错误] 文件过小 - 大小: {file.Length}字节");
            return results;
        }

        var magic = Encoding.ASCII.GetString(file, 0, 4);
        if (magic != "FTEX")
        {
            LoggerHelper.Error($"[文件错误] 无效的文件格式 - 期望: FTEX, 实际: {magic}");
            return results;
        }

        var prefix = Path.GetFileNameWithoutExtension(filePath);
        var hdsz = (int)ByteHelper.ReadUInt32(file, 8);
        var cnt = (int)ByteHelper.ReadUInt32(file, 12);

        LoggerHelper.Info($"[FTEX解析] 文件头信息 - 头大小: {hdsz}字节, 纹理数量: {cnt}");

        var st = hdsz;
        for (var i = 0; i < cnt; i++)
        {
            var p1 = 0x20 + i * 0x30;
            if (p1 + 0x20 > file.Length)
            {
                LoggerHelper.Warning($"[FTEX解析] 纹理条目超出范围 - 索引: {i}");
                break;
            }

            var fnBytes = new byte[0x20];
            Array.Copy(file, p1, fnBytes, 0, 0x20);
            fnBytes = ByteHelper.RTrim(fnBytes, 0);

            if (st + 4 > file.Length)
            {
                LoggerHelper.Warning($"[FTEX解析] 数据偏移超出范围 - 偏移: 0x{st:X}");
                break;
            }

            var ftxMagic = Encoding.ASCII.GetString(file, st, 4);
            if (ftxMagic != "FTX0")
            {
                LoggerHelper.Warning($"[FTEX解析] 无效的FTX块 - 偏移: 0x{st:X}, 魔数: {ftxMagic}");
                break;
            }

            var sz1 = (int)ByteHelper.ReadUInt32(file, st + 4);
            var sz2 = (int)ByteHelper.ReadUInt32(file, st + 8);
            var textureName = Encoding.ASCII.GetString(fnBytes).TrimEnd('\0');

            LoggerHelper.Debug($"[FTX块] 偏移: 0x{st:X}, 大小1: {sz1}, 大小2: {sz2}, 名称: {textureName}");

            Switnvt(file, st + sz2, prefix, i);
            st += sz1 + sz2;
        }

        LoggerHelper.Info($"[FTEX解析] 解析完成 - 共处理 {cnt} 个纹理");
        return results;
    }

    /// <summary>
    /// 加载CLUT格式文件
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>文件字节数据</returns>
    public static byte[] LoadClutFile(string filename)
    {
        if (!File.Exists(filename))
        {
            LoggerHelper.Warning($"[文件加载] 文件不存在 - 路径: {filename}");
            return [];
        }

        LoggerHelper.Debug($"[文件加载] 开始加载 - 路径: {filename}");

        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        var magic = new byte[4];
        var read = br.Read(magic, 0, 4);
        var magicStr = Encoding.ASCII.GetString(magic);

        switch (magicStr)
        {
            case "CLUT":
            {
                var cc = br.ReadInt32();
                var w = br.ReadInt32();
                var h = br.ReadInt32();
                var pal = br.ReadBytes(cc * 4);
                var pix = br.ReadBytes(w * h);

                var result = new byte[16 + pal.Length + pix.Length];
                "CLUT"u8.ToArray().CopyTo(result, 0);
                BitConverter.GetBytes(cc).CopyTo(result, 4);
                BitConverter.GetBytes(w).CopyTo(result, 8);
                BitConverter.GetBytes(h).CopyTo(result, 12);
                pal.CopyTo(result, 16);
                pix.CopyTo(result, 16 + pal.Length);

                LoggerHelper.Debug($"[文件加载] CLUT文件加载成功 - 尺寸: {w}x{h}, 颜色数: {cc}");
                return result;
            }
            case "RGBA":
            {
                var w = br.ReadInt32();
                var h = br.ReadInt32();
                var pix = br.ReadBytes(w * h * 4);

                var result = new byte[12 + pix.Length];
                "RGBA"u8.ToArray().CopyTo(result, 0);
                BitConverter.GetBytes(w).CopyTo(result, 4);
                BitConverter.GetBytes(h).CopyTo(result, 8);
                pix.CopyTo(result, 12);

                LoggerHelper.Debug($"[文件加载] RGBA文件加载成功 - 尺寸: {w}x{h}");
                return result;
            }
            default:
                LoggerHelper.Warning($"[文件加载] 未知的文件格式 - 魔数: {magicStr}");
                return [];
        }
    }
}
