using System.Text;
using QTSAvalonia.Helper;
using VanillawareConverter.Ftex;
using VanillawareConverter.Common;

namespace VanillawareConverter.Ftex.Textures;

/// <summary>
/// PNG格式转换器
/// </summary>
/// <remarks>
/// 提供将CLUT和RGBA格式转换为PNG图像的功能
/// </remarks>
public static class PngConverter
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// 应用PNG过滤器
    /// </summary>
    private static byte[] PngFilter(byte[] pix, int w, int h, int bytesPerPixel)
    {
        var idat = new List<byte>();
        var rowSize = w * bytesPerPixel;

        for (var y = 0; y < h; y++)
        {
            idat.Add(0);
            var offset = y * rowSize;
            for (var x = 0; x < rowSize; x++) idat.Add(pix[offset + x]);
        }

        return idat.ToArray();
    }

    /// <summary>
    /// 计算CRC32校验值
    /// </summary>
    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFF;
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
                if ((c & 1) != 0)
                    c = 0xEDB88320 ^ (c >> 1);
                else
                    c >>= 1;
            table[i] = c;
        }

        foreach (var b in data) crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Zlib压缩（存储模式）
    /// </summary>
    private static byte[] ZlibDeflateStore(byte[] data)
    {
        var zlib = new List<byte>();

        zlib.Add(0x78);
        zlib.Add(0x01);

        var len = data.Length;
        var pos = 0;
        const int BIT16 = 0xFFFF;

        while (len > BIT16)
        {
            zlib.Add(0x00);
            zlib.Add(0xFF);
            zlib.Add(0xFF);
            zlib.Add(0x00);
            zlib.Add(0x00);

            for (var i = 0; i < BIT16; i++) zlib.Add(data[pos++]);
            len -= BIT16;
        }

        var b1a = (byte)(len & 0xFF);
        var b1b = (byte)((len >> 8) & 0xFF);
        var b2a = (byte)(b1a ^ 0xFF);
        var b2b = (byte)(b1b ^ 0xFF);

        zlib.Add(0x01);
        zlib.Add(b1a);
        zlib.Add(b1b);
        zlib.Add(b2a);
        zlib.Add(b2b);

        for (var i = 0; i < len; i++) zlib.Add(data[pos++]);

        uint sum1 = 0;
        uint sum2 = 1;
        foreach (var b in data)
        {
            sum2 += b;
            while (sum2 >= 0xFFF1)
                sum2 -= 0xFFF1;
            sum1 += sum2;
            while (sum1 >= 0xFFF1)
                sum1 -= 0xFFF1;
        }

        zlib.Add((byte)((sum1 >> 8) & 0xFF));
        zlib.Add((byte)(sum1 & 0xFF));
        zlib.Add((byte)((sum2 >> 8) & 0xFF));
        zlib.Add((byte)(sum2 & 0xFF));

        return zlib.ToArray();
    }

    /// <summary>
    /// 创建PNG数据块
    /// </summary>
    private static byte[] PngChunk(string name, byte[] data, bool compress = false)
    {
        var result = new List<byte>();

        var nameBytes = Encoding.ASCII.GetBytes(name);
        byte[] chunkData;

        if (compress)
        {
            chunkData = new byte[nameBytes.Length + data.Length];
            nameBytes.CopyTo(chunkData, 0);
            data.CopyTo(chunkData, nameBytes.Length);
            chunkData = ZlibDeflateStore(chunkData[nameBytes.Length..]);

            var fullChunk = new byte[nameBytes.Length + chunkData.Length];
            nameBytes.CopyTo(fullChunk, 0);
            chunkData.CopyTo(fullChunk, nameBytes.Length);

            var len = (uint)chunkData.Length;
            result.AddRange(BitConverter.GetBytes(len).Reverse());
            result.AddRange(fullChunk);

            var crc = Crc32(fullChunk);
            result.AddRange(BitConverter.GetBytes(crc).Reverse());
        }
        else
        {
            var fullChunk = new byte[nameBytes.Length + data.Length];
            nameBytes.CopyTo(fullChunk, 0);
            data.CopyTo(fullChunk, nameBytes.Length);

            var len = (uint)data.Length;
            result.AddRange(BitConverter.GetBytes(len).Reverse());
            result.AddRange(fullChunk);

            var crc = Crc32(fullChunk);
            result.AddRange(BitConverter.GetBytes(crc).Reverse());
        }

        return result.ToArray();
    }

    /// <summary>
    /// 将CLUT格式转换为PNG
    /// </summary>
    /// <param name="file">CLUT文件数据</param>
    /// <param name="outputPath">输出路径（不含扩展名）</param>
    public static void Clut2Png(byte[] file, string outputPath)
    {
        LoggerHelper.Debug($"[PNG转换] 开始转换CLUT格式 - 输出路径: {outputPath}");

        var cc = (int)ByteHelper.ReadUInt32(file, 4);
        var w = (int)ByteHelper.ReadUInt32(file, 8);
        var h = (int)ByteHelper.ReadUInt32(file, 12);

        var plte = new List<byte>();
        var trns = new List<byte>();

        var maxColors = Math.Min(cc, 0x100);
        for (var i = 0; i < maxColors; i++)
        {
            var p = 0x10 + i * 4;
            plte.Add(file[p + 0]);
            plte.Add(file[p + 1]);
            plte.Add(file[p + 2]);
            trns.Add(file[p + 3]);
        }

        if (cc > 0x100)
        {
            LoggerHelper.Warning($"[PNG转换] 调色板颜色数超过256 - 实际: {cc}, 将截断为256色");
        }

        while (trns.Count > 0 && trns[^1] == 0xFF) trns.RemoveAt(trns.Count - 1);

        var pixelDataSize = w * h;
        var idat = new byte[pixelDataSize];
        Array.Copy(file, 0x10 + cc * 4, idat, 0, Math.Min(pixelDataSize, file.Length - (0x10 + cc * 4)));
        idat = PngFilter(idat, w, h, 1);

        var ihdr = new List<byte>();
        ihdr.AddRange(BitConverter.GetBytes((uint)w).Reverse());
        ihdr.AddRange(BitConverter.GetBytes((uint)h).Reverse());
        ihdr.Add(8);
        ihdr.Add(3);
        ihdr.Add(0);
        ihdr.Add(0);
        ihdr.Add(0);

        var png = new List<byte>();
        png.AddRange(PngMagic);
        png.AddRange(PngChunk("IHDR", ihdr.ToArray()));
        png.AddRange(PngChunk("PLTE", plte.ToArray()));

        if (trns.Count > 0) png.AddRange(PngChunk("tRNS", trns.ToArray()));

        png.AddRange(PngChunk("IDAT", idat, true));
        png.AddRange(PngChunk("IEND", []));

        File.WriteAllBytes(outputPath + ".png", png.ToArray());
        LoggerHelper.Info($"[PNG转换] CLUT转换成功 - 输出: {outputPath}.png, 尺寸: {w}x{h}, 颜色数: {Math.Min(cc, 256)}");
    }

    /// <summary>
    /// 将RGBA格式转换为PNG
    /// </summary>
    /// <param name="file">RGBA文件数据</param>
    /// <param name="outputPath">输出路径（不含扩展名）</param>
    public static void Rgba2Png(byte[] file, string outputPath)
    {
        LoggerHelper.Debug($"[PNG转换] 开始转换RGBA格式 - 输出路径: {outputPath}");

        var w = (int)ByteHelper.ReadUInt32(file, 4);
        var h = (int)ByteHelper.ReadUInt32(file, 8);

        var pixelDataSize = w * h * 4;
        var idat = new byte[pixelDataSize];
        Array.Copy(file, 12, idat, 0, Math.Min(pixelDataSize, file.Length - 12));
        idat = PngFilter(idat, w, h, 4);

        var ihdr = new List<byte>();
        ihdr.AddRange(BitConverter.GetBytes((uint)w).Reverse());
        ihdr.AddRange(BitConverter.GetBytes((uint)h).Reverse());
        ihdr.Add(8);
        ihdr.Add(6);
        ihdr.Add(0);
        ihdr.Add(0);
        ihdr.Add(0);

        var png = new List<byte>();
        png.AddRange(PngMagic);
        png.AddRange(PngChunk("IHDR", ihdr.ToArray()));
        png.AddRange(PngChunk("IDAT", idat, true));
        png.AddRange(PngChunk("IEND", []));

        File.WriteAllBytes(outputPath + ".png", png.ToArray());
        LoggerHelper.Info($"[PNG转换] RGBA转换成功 - 输出: {outputPath}.png, 尺寸: {w}x{h}");
    }

    /// <summary>
    /// 将图像文件转换为PNG
    /// </summary>
    /// <param name="filePath">图像文件路径</param>
    public static void Img2Png(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LoggerHelper.Error($"[PNG转换] 文件不存在 - 路径: {filePath}");
            return;
        }

        LoggerHelper.Debug($"[PNG转换] 开始转换 - 文件: {filePath}");

        var file = File.ReadAllBytes(filePath);
        if (file.Length < 4)
        {
            LoggerHelper.Error($"[PNG转换] 文件过小 - 大小: {file.Length}字节");
            return;
        }

        var magic = Encoding.ASCII.GetString(file, 0, 4);

        switch (magic)
        {
            case "CLUT":
                Clut2Png(file, Path.ChangeExtension(filePath, null));
                break;
            case "RGBA":
                Rgba2Png(file, Path.ChangeExtension(filePath, null));
                break;
            default:
                LoggerHelper.Warning($"[PNG转换] 未知的图像格式 - 格式: {magic}");
                break;
        }
    }

    /// <summary>
    /// 将NVT文件转换为PNG
    /// </summary>
    /// <param name="nvtFilePath">NVT文件路径</param>
    public static void ConvertNvtToPng(string nvtFilePath)
    {
        if (!File.Exists(nvtFilePath))
        {
            LoggerHelper.Error($"[PNG转换] NVT文件不存在 - 路径: {nvtFilePath}");
            return;
        }

        LoggerHelper.Debug($"[PNG转换] 开始转换NVT文件 - 文件: {nvtFilePath}");

        var file = File.ReadAllBytes(nvtFilePath);
        if (file.Length < 4)
        {
            LoggerHelper.Error($"[PNG转换] NVT文件过小 - 大小: {file.Length}字节");
            return;
        }

        var magic = Encoding.ASCII.GetString(file, 0, 4);
        var outputPath = Path.ChangeExtension(nvtFilePath, null);

        switch (magic)
        {
            case "CLUT":
                Clut2Png(file, outputPath);
                break;
            case "RGBA":
                Rgba2Png(file, outputPath);
                break;
            default:
                LoggerHelper.Warning($"[PNG转换] 未知的NVT格式 - 格式: {magic}");
                break;
        }
    }

    /// <summary>
    /// 批量转换目录中的NVT文件
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="pattern">文件匹配模式</param>
    public static void ConvertDirectory(string directoryPath, string pattern = "*.nvt")
    {
        if (!Directory.Exists(directoryPath))
        {
            LoggerHelper.Error($"[PNG转换] 目录不存在 - 路径: {directoryPath}");
            return;
        }

        LoggerHelper.Info($"[PNG转换] 开始批量转换 - 目录: {directoryPath}, 匹配模式: {pattern}");

        var files = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
        LoggerHelper.Info($"[PNG转换] 找到 {files.Length} 个匹配文件");

        var successCount = 0;
        var failCount = 0;

        foreach (var file in files)
            try
            {
                ConvertNvtToPng(file);
                successCount++;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[PNG转换] 转换失败 - 文件: {Path.GetFileName(file)}", ex);
                failCount++;
            }

        LoggerHelper.Info($"[PNG转换] 批量转换完成 - 成功: {successCount}, 失败: {failCount}, 总计: {files.Length}");
    }
}
