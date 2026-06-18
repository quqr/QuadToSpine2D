namespace VanillawareConverter.Ftex;

/// <summary>
/// 提供字节数组操作的工具类
/// </summary>
/// <remarks>
/// <para>
/// ByteHelper类包含用于字节数组读取、写入和转换的静态方法。
/// 这些方法主要用于处理二进制文件格式，如FTEX纹理文件。
/// </para>
/// <para>
/// 所有方法都是静态的，可直接调用而无需实例化。
/// </para>
/// </remarks>
public static class ByteHelper
{
    /// <summary>
    /// 创建指定长度的零填充字节数组
    /// </summary>
    /// <param name="count">数组长度</param>
    /// <returns>填充为零的字节数组</returns>
    public static byte[] ZeroBytes(int count)
    {
        return new byte[count];
    }

    /// <summary>
    /// 创建用指定值填充的字节数组
    /// </summary>
    /// <param name="value">填充值</param>
    /// <param name="count">数组长度</param>
    /// <returns>用指定值填充的字节数组</returns>
    public static byte[] ByteRepeat(byte value, int count)
    {
        var result = new byte[count];
        Array.Fill(result, value);
        return result;
    }

    /// <summary>
    /// 从字节数组中读取16位无符号整数（小端序）
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <returns>读取的16位无符号整数</returns>
    public static ushort ReadUInt16(byte[] data, int pos)
    {
        return (ushort)(data[pos] | (data[pos + 1] << 8));
    }

    /// <summary>
    /// 从字节数组中读取32位无符号整数（小端序）
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <returns>读取的32位无符号整数</returns>
    public static uint ReadUInt32(byte[] data, int pos)
    {
        return (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
    }

    /// <summary>
    /// 将16位无符号整数写入字节数组（小端序）
    /// </summary>
    /// <param name="data">目标字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <param name="value">要写入的值</param>
    public static void WriteUInt16(byte[] data, int pos, ushort value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>
    /// 将32位无符号整数写入字节数组（小端序）
    /// </summary>
    /// <param name="data">目标字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <param name="value">要写入的值</param>
    public static void WriteUInt32(byte[] data, int pos, uint value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
        data[pos + 2] = (byte)((value >> 16) & 0xFF);
        data[pos + 3] = (byte)((value >> 24) & 0xFF);
    }

    /// <summary>
    /// 将整数值限制在指定范围内
    /// </summary>
    /// <param name="value">要限制的值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>限制后的值</returns>
    public static int IntClamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>
    /// 计算大于或等于指定值的最小2的幂
    /// </summary>
    /// <param name="value">输入值</param>
    /// <returns>大于或等于输入值的最小2的幂</returns>
    public static int IntCeilPow2(int value)
    {
        if (value <= 0) return 1;
        var ceil = 1;
        while (ceil < value)
            ceil <<= 1;
        return ceil;
    }

    /// <summary>
    /// 计算向上取整的结果
    /// </summary>
    /// <param name="value">被除数</param>
    /// <param name="divisor">除数</param>
    /// <returns>向上取整后的结果</returns>
    /// <remarks>
    /// 如果除数为0，返回0。
    /// 如果除数为正数，返回大于或等于商的最小整数。
    /// 如果除数为负数，返回小于或等于商的最大整数。
    /// </remarks>
    public static int IntCeil(int value, int divisor)
    {
        switch (divisor)
        {
            case 0:
                return 0;
            case > 0:
            {
                while (value % divisor != 0)
                    value++;
                return value;
            }
        }

        divisor = -divisor;
        while (value % divisor != 0)
            value--;
        return value;
    }

    /// <summary>
    /// 将源字节数组复制到目标数组的指定位置
    /// </summary>
    /// <param name="dest">目标字节数组</param>
    /// <param name="pos">目标起始位置</param>
    /// <param name="src">源字节数组</param>
    public static void StrUpdate(byte[] dest, int pos, byte[] src)
    {
        Array.Copy(src, 0, dest, pos, src.Length);
    }

    /// <summary>
    /// 从字节数组中提取子数组
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <param name="length">提取长度</param>
    /// <returns>提取的子数组</returns>
    public static byte[] Substr(byte[] data, int pos, int length)
    {
        var result = new byte[length];
        Array.Copy(data, pos, result, 0, length);
        return result;
    }

    /// <summary>
    /// 从右侧移除指定字符
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="trimChar">要移除的字符</param>
    /// <returns>移除指定字符后的数组</returns>
    public static byte[] RTrim(byte[] data, byte trimChar)
    {
        var end = data.Length - 1;
        while (end >= 0 && data[end] == trimChar)
            end--;
        if (end < 0) return [];
        var result = new byte[end + 1];
        Array.Copy(data, result, end + 1);
        return result;
    }

    /// <summary>
    /// 生成灰度调色板
    /// </summary>
    /// <param name="count">颜色数量</param>
    /// <returns>RGBA格式的调色板数据</returns>
    public static byte[] GrayClut(int count)
    {
        return GradientClut(count, [0, 0, 0, 0], [255, 255, 255, 255]);
    }

    /// <summary>
    /// 生成渐变调色板
    /// </summary>
    /// <param name="count">颜色数量</param>
    /// <param name="src">起始颜色（RGBA格式）</param>
    /// <param name="dst">结束颜色（RGBA格式）</param>
    /// <returns>RGBA格式的调色板数据</returns>
    /// <exception cref="ArgumentException">当count小于等于1时抛出</exception>
    public static byte[] GradientClut(int count, byte[] src, byte[] dst)
    {
        if (count <= 1)
            throw new ArgumentException("count must be greater than 1");

        double r1 = src[0];
        double g1 = src[1];
        double b1 = src[2];
        double a1 = src[3];

        var sr = (dst[0] - r1) / (count - 1);
        var sg = (dst[1] - g1) / (count - 1);
        var sb = (dst[2] - b1) / (count - 1);
        var sa = (dst[3] - a1) / (count - 1);

        var clut = new byte[count * 4];
        for (var i = 0; i < count; i++)
        {
            clut[i * 4 + 0] = (byte)IntClamp((int)Math.Round(r1), 0, 255);
            clut[i * 4 + 1] = (byte)IntClamp((int)Math.Round(g1), 0, 255);
            clut[i * 4 + 2] = (byte)IntClamp((int)Math.Round(b1), 0, 255);
            clut[i * 4 + 3] = (byte)IntClamp((int)Math.Round(a1), 0, 255);
            r1 += sr;
            g1 += sg;
            b1 += sb;
            a1 += sa;
        }

        return clut;
    }
}
