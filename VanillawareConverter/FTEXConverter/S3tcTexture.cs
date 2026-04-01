using VanillawareConverter.Ftex;

namespace VanillawareConverter.Ftex.Textures;

public class S3tcTexture
{
    private static ushort ReadInt16(byte[] data, int pos)
    {
        return (ushort)(data[pos] | (data[pos + 1] << 8));
    }

    private static int[] Bc1Index(byte[] data, int pos)
    {
        var index = new int[16];
        for (var i = 0; i < 4; i++)
        {
            var b1 = data[pos++];
            for (var j = 0; j < 8; j += 2) index[i * 4 + j / 2] = (b1 >> j) & 3;
        }

        return index;
    }

    private static int[] Bc3Index(byte[] data, int pos)
    {
        var index = new int[16];
        for (var i = 0; i < 6; i += 3)
        {
            var b1 = data[pos];
            var b2 = data[pos + 1];
            var b3 = data[pos + 2];
            pos += 3;

            var intVal = (uint)(b1 | (b2 << 8) | (b3 << 16));
            for (var j = 0; j < 24; j += 3) index[i / 3 * 8 + j / 3] = (int)((intVal >> j) & 7);
        }

        return index;
    }

    private static byte[] Rgb565(ushort intVal)
    {
        var r = (byte)((intVal >> 8) & 0xf8);
        var g = (byte)((intVal >> 3) & 0xfc);
        var b = (byte)((intVal << 3) & 0xf8);
        return [r, g, b, 255];
    }

    private static byte[] RgbInterpolate(byte[] rgb1, double fact1, byte[] rgb2, double fact2)
    {
        var r = (byte)ByteHelper.IntClamp((int)Math.Round(rgb1[0] * fact1 + rgb2[0] * fact2), 0, 255);
        var g = (byte)ByteHelper.IntClamp((int)Math.Round(rgb1[1] * fact1 + rgb2[1] * fact2), 0, 255);
        var b = (byte)ByteHelper.IntClamp((int)Math.Round(rgb1[2] * fact1 + rgb2[2] * fact2), 0, 255);
        return [r, g, b, 255];
    }

    private static byte[] RgbaMix(byte[][] rgb, int[] alp)
    {
        var pix = new byte[64];
        for (var i = 0; i < 16; i++)
        {
            pix[i * 4 + 0] = (byte)ByteHelper.IntClamp(rgb[i][0], 0, 255);
            pix[i * 4 + 1] = (byte)ByteHelper.IntClamp(rgb[i][1], 0, 255);
            pix[i * 4 + 2] = (byte)ByteHelper.IntClamp(rgb[i][2], 0, 255);
            pix[i * 4 + 3] = (byte)ByteHelper.IntClamp(alp[i], 0, 255);
        }

        return pix;
    }

    private static byte[][] Bc1Color(byte[] data, int pos, int bc)
    {
        var int1 = ReadInt16(data, pos);
        var int2 = ReadInt16(data, pos + 2);
        var index = Bc1Index(data, pos + 4);

        var rgb = new byte[4][];
        rgb[0] = Rgb565(int1);
        rgb[1] = Rgb565(int2);

        if (bc != 1 || int1 > int2)
        {
            rgb[2] = RgbInterpolate(rgb[0], 2.0 / 3.0, rgb[1], 1.0 / 3.0);
            rgb[3] = RgbInterpolate(rgb[0], 1.0 / 3.0, rgb[1], 2.0 / 3.0);
        }
        else
        {
            rgb[2] = RgbInterpolate(rgb[0], 0.5, rgb[1], 0.5);
            rgb[3] = [0, 0, 0, 0];
        }

        var pix = new byte[16][];
        for (var i = 0; i < 16; i++) pix[i] = rgb[index[i]];
        return pix;
    }

    private static int[] Bc2Alpha(byte[] data, int pos)
    {
        var alp = new int[16];
        for (var i = 0; i < 8; i++)
        {
            var b = data[pos++];
            var b1 = b & 0x0F;
            var b2 = (b >> 4) & 0x0F;
            alp[i * 2] = b1 * 0x11;
            alp[i * 2 + 1] = b2 * 0x11;
        }

        return alp;
    }

    private static int[] Bc3Alpha(byte[] data, int pos)
    {
        var a = new int[8];
        a[0] = data[pos];
        a[1] = data[pos + 1];
        var index = Bc3Index(data, pos + 2);

        if (a[0] > a[1])
        {
            a[2] = (int)Math.Round(a[0] * 6.0 / 7.0 + a[1] * 1.0 / 7.0);
            a[3] = (int)Math.Round(a[0] * 5.0 / 7.0 + a[1] * 2.0 / 7.0);
            a[4] = (int)Math.Round(a[0] * 4.0 / 7.0 + a[1] * 3.0 / 7.0);
            a[5] = (int)Math.Round(a[0] * 3.0 / 7.0 + a[1] * 4.0 / 7.0);
            a[6] = (int)Math.Round(a[0] * 2.0 / 7.0 + a[1] * 5.0 / 7.0);
            a[7] = (int)Math.Round(a[0] * 1.0 / 7.0 + a[1] * 6.0 / 7.0);
        }
        else
        {
            a[2] = (int)Math.Round(a[0] * 4.0 / 5.0 + a[1] * 1.0 / 5.0);
            a[3] = (int)Math.Round(a[0] * 3.0 / 5.0 + a[1] * 2.0 / 5.0);
            a[4] = (int)Math.Round(a[0] * 2.0 / 5.0 + a[1] * 3.0 / 5.0);
            a[5] = (int)Math.Round(a[0] * 1.0 / 5.0 + a[1] * 4.0 / 5.0);
            a[6] = 0;
            a[7] = 255;
        }

        var pix = new int[16];
        for (var i = 0; i < 16; i++) pix[i] = a[index[i]];
        return pix;
    }

    public byte[] Bc1(byte[] data)
    {
        var pix = new List<byte>();
        var len = ByteHelper.IntCeil(data.Length, 8);
        for (var i = 0; i < len; i += 8)
        {
            var rgb = Bc1Color(data, i, 1);
            var alp = new int[16];
            for (var j = 0; j < 16; j++) alp[j] = rgb[j][3];
            pix.AddRange(RgbaMix(rgb, alp));
        }

        return pix.ToArray();
    }

    public byte[] Bc2(byte[] data)
    {
        var pix = new List<byte>();
        var len = ByteHelper.IntCeil(data.Length, 16);
        for (var i = 0; i < len; i += 16)
        {
            var alp = Bc2Alpha(data, i);
            var rgb = Bc1Color(data, i + 8, 2);
            pix.AddRange(RgbaMix(rgb, alp));
        }

        return pix.ToArray();
    }

    public byte[] Bc3(byte[] data)
    {
        var pix = new List<byte>();
        var len = ByteHelper.IntCeil(data.Length, 16);
        for (var i = 0; i < len; i += 16)
        {
            var alp = Bc3Alpha(data, i);
            var rgb = Bc1Color(data, i + 8, 3);
            pix.AddRange(RgbaMix(rgb, alp));
        }

        return pix.ToArray();
    }

    public byte[] Bc4(byte[] data)
    {
        var pix = new List<byte>();
        var len = ByteHelper.IntCeil(data.Length, 8);
        for (var i = 0; i < len; i += 8)
        {
            var alp = Bc3Alpha(data, i);
            for (var j = 0; j < 16; j++)
            {
                var a = (byte)ByteHelper.IntClamp(alp[j], 0, 255);
                pix.Add(a);
            }
        }

        return pix.ToArray();
    }

    public byte[] Dxt1(byte[] data)
    {
        return Bc1(data);
    }

    public byte[] Dxt2(byte[] data)
    {
        return Bc2(data);
    }

    public byte[] Dxt3(byte[] data)
    {
        return Bc2(data);
    }

    public byte[] Dxt4(byte[] data)
    {
        return Bc3(data);
    }

    public byte[] Dxt5(byte[] data)
    {
        return Bc3(data);
    }
}