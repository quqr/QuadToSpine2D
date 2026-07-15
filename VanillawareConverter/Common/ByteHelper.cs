using System.Text;

namespace VanillawareConverter.Common;

/// <summary>
/// 字节数组操作工具类，提供读取、写入和转换的静态方法。
/// 支持大端和小端字节序，用于处理二进制文件格式（FTEX 纹理、MBS 动画等）。
/// </summary>
public static class ByteHelper
{
    #region 数组创建

    /// <summary>
    /// 创建指定长度的零填充字节数组
    /// </summary>
    public static byte[] ZeroBytes(int count) => new byte[count];

    /// <summary>
    /// 创建用指定值填充的字节数组
    /// </summary>
    public static byte[] ByteRepeat(byte value, int count)
    {
        var result = new byte[count];
        Array.Fill(result, value);
        return result;
    }

    #endregion

    #region 读取方法

    /// <summary>
    /// 从字节数组中读取32位浮点数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false（小端）</param>
    public static float ReadFloat32(byte[] data, int offset, bool bigEndian = false)
    {
        if (offset + 4 > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        var bytes = new byte[4];
        if (bigEndian)
        {
            bytes[0] = data[offset + 3];
            bytes[1] = data[offset + 2];
            bytes[2] = data[offset + 1];
            bytes[3] = data[offset];
        }
        else
        {
            Array.Copy(data, offset, bytes, 0, 4);
        }

        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    /// 从字节数组中读取16位有符号整数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false（小端）</param>
    public static short ReadInt16(byte[] data, int offset, bool bigEndian = false)
    {
        if (offset + 2 > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (bigEndian) return (short)((data[offset] << 8) | data[offset + 1]);
        return (short)(data[offset] | (data[offset + 1] << 8));
    }

    /// <summary>
    /// 从字节数组中读取16位无符号整数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false（小端）</param>
    public static ushort ReadUInt16(byte[] data, int pos, bool bigEndian = false)
    {
        return (ushort)ReadInt16(data, pos, bigEndian);
    }

    /// <summary>
    /// 从字节数组中读取32位有符号整数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false（小端）</param>
    public static int ReadInt32(byte[] data, int offset, bool bigEndian = false)
    {
        if (offset + 4 > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (bigEndian)
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    /// <summary>
    /// 从字节数组中读取32位无符号整数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="pos">起始位置</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false（小端）</param>
    public static uint ReadUInt32(byte[] data, int pos, bool bigEndian = false)
    {
        return (uint)ReadInt32(data, pos, bigEndian);
    }

    /// <summary>
    /// 从字节数组中读取指定字节数的整数值
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="byteCount">要读取的字节数（1、2或4）</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为 false</param>
    /// <param name="signed">是否作为有符号整数读取，默认为 false</param>
    public static int ReadInt(byte[] data, int offset, int byteCount, bool bigEndian = false, bool signed = false)
    {
        if (byteCount == 1) return signed ? (sbyte)data[offset] : data[offset];

        if (byteCount == 2)
        {
            var val = ReadInt16(data, offset, bigEndian);
            return signed ? val : (ushort)val;
        }

        if (byteCount == 4)
        {
            var val = ReadInt32(data, offset, bigEndian);
            return signed ? val : (int)(uint)val;
        }

        throw new ArgumentException("byteCount must be 1, 2, or 4", nameof(byteCount));
    }

    #endregion

    #region 写入方法

    /// <summary>
    /// 将16位无符号整数写入字节数组（小端序）
    /// </summary>
    public static void WriteUInt16(byte[] data, int pos, ushort value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>
    /// 将32位无符号整数写入字节数组（小端序）
    /// </summary>
    public static void WriteUInt32(byte[] data, int pos, uint value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
        data[pos + 2] = (byte)((value >> 16) & 0xFF);
        data[pos + 3] = (byte)((value >> 24) & 0xFF);
    }

    #endregion

    #region 字符串方法

    /// <summary>
    /// 从字节数组中读取以 null 结尾的字符串
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="maxLength">最大读取长度，默认为 256</param>
    public static string ReadNullTerminatedString(byte[] data, int offset, int maxLength = 256)
    {
        if (offset >= data.Length)
            return string.Empty;

        var length = 0;
        while (offset + length < data.Length && length < maxLength && data[offset + length] != 0) length++;

        return Encoding.UTF8.GetString(data, offset, length);
    }

    /// <summary>
    /// 将字节数组转换为十六进制字符串
    /// </summary>
    public static string ReadHexString(byte[] data, int offset, int length)
    {
        if (offset + length > data.Length)
            return string.Empty;

        var sb = new StringBuilder(length * 2);
        for (var i = 0; i < length; i++) sb.Append(data[offset + i].ToString("x2"));
        return sb.ToString();
    }

    /// <summary>
    /// 将字节数组转换为带"#"前缀的十六进制字符串
    /// </summary>
    public static string ReadHexStringWithPrefix(byte[] data, int offset, int length)
    {
        return "#" + ReadHexString(data, offset, length);
    }

    #endregion

    #region 数组操作

    /// <summary>
    /// 将源字节数组复制到目标数组的指定位置
    /// </summary>
    public static void StrUpdate(byte[] dest, int pos, byte[] src)
    {
        Array.Copy(src, 0, dest, pos, src.Length);
    }

    /// <summary>
    /// 从字节数组中提取子数组
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始位置</param>
    /// <param name="length">提取长度</param>
    public static byte[] SubArray(byte[] data, int offset, int length)
    {
        if (offset + length > data.Length)
            length = data.Length - offset;

        var result = new byte[length];
        Array.Copy(data, offset, result, 0, length);
        return result;
    }

    /// <summary>
    /// 从字节数组中提取子数组（SubArray 的别名）
    /// </summary>
    public static byte[] Substr(byte[] data, int pos, int length) => SubArray(data, pos, length);

    /// <summary>
    /// 从右侧移除指定字符
    /// </summary>
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

    #endregion

    #region 数学辅助

    /// <summary>
    /// 将整数值限制在指定范围内
    /// </summary>
    public static int IntClamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>
    /// 计算大于或等于指定值的最小2的幂
    /// </summary>
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

    #endregion

    #region 调色板生成

    /// <summary>
    /// 生成灰度调色板
    /// </summary>
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

    #endregion
}
