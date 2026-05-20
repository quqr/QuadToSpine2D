using System.Text;

namespace VanillawareConverter.Mbs;

/// <summary>
/// 字节数据读取辅助类
/// </summary>
/// <remarks>
/// 提供从字节数组中读取各种数据类型的静态方法，
/// 支持大端和小端字节序
/// </remarks>
public static class ByteHelper
{
    /// <summary>
    /// 从字节数组中读取32位浮点数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <returns>读取的32位浮点数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当偏移量超出范围时抛出</exception>
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
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <returns>读取的16位有符号整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当偏移量超出范围时抛出</exception>
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
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <returns>读取的16位无符号整数</returns>
    public static ushort ReadUInt16(byte[] data, int offset, bool bigEndian = false)
    {
        return (ushort)ReadInt16(data, offset, bigEndian);
    }

    /// <summary>
    /// 从字节数组中读取32位有符号整数
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <returns>读取的32位有符号整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当偏移量超出范围时抛出</exception>
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
    /// <param name="offset">起始偏移量</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <returns>读取的32位无符号整数</returns>
    public static uint ReadUInt32(byte[] data, int offset, bool bigEndian = false)
    {
        return (uint)ReadInt32(data, offset, bigEndian);
    }

    /// <summary>
    /// 从字节数组中读取指定字节数的整数值
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="byteCount">要读取的字节数（1、2或4）</param>
    /// <param name="bigEndian">是否使用大端字节序，默认为false（小端）</param>
    /// <param name="signed">是否作为有符号整数读取，默认为false</param>
    /// <returns>读取的整数值</returns>
    /// <exception cref="ArgumentException">当byteCount不是1、2或4时抛出</exception>
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

    /// <summary>
    /// 从字节数组中读取以null结尾的字符串
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="maxLength">最大读取长度，默认为256</param>
    /// <returns>读取的UTF-8编码字符串</returns>
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
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="length">要转换的字节长度</param>
    /// <returns>十六进制字符串（小写）</returns>
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
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="length">要转换的字节长度</param>
    /// <returns>带"#"前缀的十六进制字符串</returns>
    public static string ReadHexStringWithPrefix(byte[] data, int offset, int length)
    {
        return "#" + ReadHexString(data, offset, length);
    }

    /// <summary>
    /// 从字节数组中提取子数组
    /// </summary>
    /// <param name="data">源字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="length">要提取的长度</param>
    /// <returns>提取的子数组</returns>
    public static byte[] SubArray(byte[] data, int offset, int length)
    {
        if (offset + length > data.Length)
            length = data.Length - offset;

        var result = new byte[length];
        Array.Copy(data, offset, result, 0, length);
        return result;
    }
}
