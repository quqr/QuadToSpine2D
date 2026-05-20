using QTSAvalonia.Helper;

namespace VanillawareConverter.Ftex.Swizzling;

/// <summary>
/// Tegra X1 (Nintendo Switch) 纹理扭曲算法
/// </summary>
/// <remarks>
/// 用于处理Nintendo Switch平台纹理数据的扭曲/解扭曲操作
/// </remarks>
public static class TegraX1Swizzle
{
    /// <summary>
    /// 位掩码查找表
    /// </summary>
    /// <remarks>
    /// 每个元素包含：[块数量阈值, X坐标掩码, Y坐标掩码]
    /// </remarks>
    private static readonly int[][] BitMasks =
    [
        [0x40, 0x32, 0xd],
        [0x100, 0xd2, 0x2d],
        [0x400, 0x392, 0x6d],
        [0x1000, 0xf12, 0xed],
        [0x4000, 0x3e12, 0x1ed],
        [0x10000, 0x7e12, 0x81ed],
        [0x40000, 0xfe12, 0x301ed],
        [0x100000, 0x1fe12, 0xe01ed]
    ];

    /// <summary>
    /// 应用位掩码进行坐标变换
    /// </summary>
    /// <param name="value">原始坐标值</param>
    /// <param name="mask">位掩码</param>
    /// <returns>变换后的坐标值</returns>
    public static int SwizzleBitmask(int value, int mask)
    {
        var result = 0;
        var shift = 0;

        while (true)
        {
            if (mask < 1) break;
            if (value < 1) break;

            var bitValue = value & 1;
            var bitMask = mask & 1;
            value >>= 1;
            mask >>= 1;

            if (bitMask != 0)
            {
                result |= bitValue << shift;
                shift++;
            }
        }

        return result;
    }

    /// <summary>
    /// 复制4x4像素块到目标位置
    /// </summary>
    private static void PixdecCopy44(byte[] pix, byte[] dec, ref int pos, int dx, int dy, int width, int height,
        int bpp)
    {
        if (dx >= width) return;
        if (dy >= height) return;

        var row = 4 * bpp;
        for (var y = 0; y < 4; y++)
        {
            var dyy = (dy * 4 + y) * width * 4;
            var dxx = dx * 4 + dyy;
            for (var x = 0; x < row; x++)
            {
                if (pos >= pix.Length) break;
                dec[dxx * bpp + x] = pix[pos++];
            }
        }
    }

    /// <summary>
    /// 解扭曲8位纹理数据
    /// </summary>
    /// <param name="pix">扭曲的纹理数据</param>
    /// <param name="ow">原始宽度</param>
    /// <param name="oh">原始高度</param>
    /// <returns>解扭曲后的纹理数据</returns>
    public static byte[] Swizzle8Bits(byte[] pix, int ow, int oh)
    {
        LoggerHelper.Debug($"[扭曲处理] 开始8位纹理解扭曲 - 尺寸: {ow}x{oh}");

        var width = ow >> 2;
        var height = oh >> 2;
        var bpp = 1;

        var dec = new byte[ow * oh];
        var pos = 0;

        var lenPix = pix.Length;
        var lenBlk = lenPix >> 4;

        LoggerHelper.Debug($"[扭曲处理] 数据长度: {lenPix}字节, 块数量: {lenBlk}");

        foreach (var bv in BitMasks)
            if (lenBlk <= bv[0])
            {
                LoggerHelper.Debug($"[扭曲处理] 使用位掩码 - X掩码: 0x{bv[1]:X}, Y掩码: 0x{bv[2]:X}");
                var i = 0;
                while (i < bv[0] && pos < lenPix)
                {
                    var x = SwizzleBitmask(i >> 1, bv[1]) << 1;
                    var y = SwizzleBitmask(i >> 1, bv[2]);
                    PixdecCopy44(pix, dec, ref pos, x + 0, y, width, height, bpp);
                    PixdecCopy44(pix, dec, ref pos, x + 1, y, width, height, bpp);
                    i += 2;
                }

                LoggerHelper.Debug($"[扭曲处理] 8位纹理解扭曲完成 - 处理块数: {i}");
                return dec;
            }

        LoggerHelper.Warning($"[扭曲处理] 纹理尺寸过大，跳过解扭曲 - 尺寸: {ow}x{oh} (超过400x400块或1000x1000像素)");
        return pix;
    }

    /// <summary>
    /// 解扭曲16位纹理数据
    /// </summary>
    /// <param name="pix">扭曲的纹理数据</param>
    /// <param name="ow">原始宽度</param>
    /// <param name="oh">原始高度</param>
    /// <returns>解扭曲后的纹理数据</returns>
    public static byte[] Swizzle16Bits(byte[] pix, int ow, int oh)
    {
        LoggerHelper.Debug($"[扭曲处理] 开始16位纹理解扭曲 - 尺寸: {ow}x{oh}");

        var width = ow >> 2;
        var height = oh >> 2;
        var bpp = 4;

        var dec = new byte[ow * oh * 4];
        var pos = 0;

        var lenPix = pix.Length;
        var lenBlk = lenPix >> 6;

        LoggerHelper.Debug($"[扭曲处理] 数据长度: {lenPix}字节, 块数量: {lenBlk}");

        foreach (var bv in BitMasks)
            if (lenBlk <= bv[0])
            {
                LoggerHelper.Debug($"[扭曲处理] 使用位掩码 - X掩码: 0x{bv[1]:X}, Y掩码: 0x{bv[2]:X}");
                var i = 0;
                while (i < bv[0] && pos < lenPix)
                {
                    var x = SwizzleBitmask(i, bv[1]);
                    var y = SwizzleBitmask(i, bv[2]);
                    PixdecCopy44(pix, dec, ref pos, x, y, width, height, bpp);
                    i++;
                }

                LoggerHelper.Debug($"[扭曲处理] 16位纹理解扭曲完成 - 处理块数: {i}");
                return dec;
            }

        LoggerHelper.Warning($"[扭曲处理] 纹理尺寸过大，跳过解扭曲 - 尺寸: {ow}x{oh} (超过400x400块或1000x1000像素)");
        return pix;
    }
}
