namespace VanillawareConverter.Ftex;

public static class ByteHelper
{
    public static byte[] ZeroBytes(int count)
    {
        return new byte[count];
    }

    public static byte[] ByteRepeat(byte value, int count)
    {
        var result = new byte[count];
        Array.Fill(result, value);
        return result;
    }

    public static ushort ReadUInt16(byte[] data, int pos)
    {
        return (ushort)(data[pos] | (data[pos + 1] << 8));
    }

    public static uint ReadUInt32(byte[] data, int pos)
    {
        return (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
    }

    public static void WriteUInt16(byte[] data, int pos, ushort value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
    }

    public static void WriteUInt32(byte[] data, int pos, uint value)
    {
        data[pos] = (byte)(value & 0xFF);
        data[pos + 1] = (byte)((value >> 8) & 0xFF);
        data[pos + 2] = (byte)((value >> 16) & 0xFF);
        data[pos + 3] = (byte)((value >> 24) & 0xFF);
    }

    public static int IntClamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static int IntCeilPow2(int value)
    {
        if (value <= 0) return 0;
        var ceil = 1;
        while (ceil < value)
            ceil <<= 1;
        return ceil;
    }

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

    public static void StrUpdate(byte[] dest, int pos, byte[] src)
    {
        Array.Copy(src, 0, dest, pos, src.Length);
    }

    public static byte[] Substr(byte[] data, int pos, int length)
    {
        var result = new byte[length];
        Array.Copy(data, pos, result, 0, length);
        return result;
    }

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

    public static byte[] GrayClut(int count)
    {
        return GradientClut(count, [0, 0, 0, 0], [255, 255, 255, 255]);
    }

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