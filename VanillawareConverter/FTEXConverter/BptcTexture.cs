using VanillawareConverter.Common;

namespace VanillawareConverter.Ftex.Textures;

public class BptcTexture
{
    private static readonly int[] P2Table =
    [
        0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1,
        0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1,
        0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1,
        0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 1,
        0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1,
        0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1,
        0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1,
        0, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0,
        0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0,
        0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0,
        0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 1,
        0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0,
        0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0,
        0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0,
        0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 0, 0,
        0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0,
        0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0,
        0, 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 0,
        0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0,
        0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1,
        0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1,
        0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0,
        0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0,
        0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0,
        0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1,
        0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1,
        0, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 0,
        0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0,
        0, 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 1, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0,
        0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0,
        0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1,
        0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1,
        0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0,
        0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0,
        0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0,
        0, 1, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1,
        0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1,
        0, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0,
        0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 0,
        0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0, 1,
        0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1,
        0, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1,
        0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1,
        0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0,
        0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0,
        0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1
    ];

    private static readonly int[] P3Table =
    [
        0, 0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 1, 2, 2, 2, 2,
        0, 0, 0, 1, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 2, 1,
        0, 0, 0, 0, 2, 0, 0, 1, 2, 2, 1, 1, 2, 2, 1, 1,
        0, 2, 2, 2, 0, 0, 2, 2, 0, 0, 1, 1, 0, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2,
        0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 2, 2, 0, 0, 2, 2,
        0, 0, 2, 2, 0, 0, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 1, 1, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
        0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
        0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
        0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2,
        0, 1, 1, 2, 0, 1, 1, 2, 0, 1, 1, 2, 0, 1, 1, 2,
        0, 1, 2, 2, 0, 1, 2, 2, 0, 1, 2, 2, 0, 1, 2, 2,
        0, 0, 1, 1, 0, 1, 1, 2, 1, 1, 2, 2, 1, 2, 2, 2,
        0, 0, 1, 1, 2, 0, 0, 1, 2, 2, 0, 0, 2, 2, 2, 0,
        0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 2, 1, 1, 2, 2,
        0, 1, 1, 1, 0, 0, 1, 1, 2, 0, 0, 1, 2, 2, 0, 0,
        0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2,
        0, 0, 2, 2, 0, 0, 2, 2, 0, 0, 2, 2, 1, 1, 1, 1,
        0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2, 0, 2, 2, 2,
        0, 0, 0, 1, 0, 0, 0, 1, 2, 2, 2, 1, 2, 2, 2, 1,
        0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 2, 2, 0, 1, 2, 2,
        0, 0, 0, 0, 1, 1, 0, 0, 2, 2, 1, 0, 2, 2, 1, 0,
        0, 1, 2, 2, 0, 1, 2, 2, 0, 0, 1, 1, 0, 0, 0, 0,
        0, 0, 1, 2, 0, 0, 1, 2, 1, 1, 2, 2, 2, 2, 2, 2,
        0, 1, 1, 0, 1, 2, 2, 1, 1, 2, 2, 1, 0, 1, 1, 0,
        0, 0, 0, 0, 0, 1, 1, 0, 1, 2, 2, 1, 1, 2, 2, 1,
        0, 0, 2, 2, 1, 1, 0, 2, 1, 1, 0, 2, 0, 0, 2, 2,
        0, 1, 1, 0, 0, 1, 1, 0, 2, 0, 0, 2, 2, 2, 2, 2,
        0, 0, 1, 1, 0, 1, 2, 2, 0, 1, 2, 2, 0, 0, 1, 1,
        0, 0, 0, 0, 2, 0, 0, 0, 2, 2, 1, 1, 2, 2, 2, 1,
        0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 2, 2, 2,
        0, 2, 2, 2, 0, 0, 2, 2, 0, 0, 1, 2, 0, 0, 1, 1,
        0, 0, 1, 1, 0, 0, 1, 2, 0, 0, 2, 2, 0, 2, 2, 2,
        0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0,
        0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 0, 0, 0, 0,
        0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0,
        0, 1, 2, 0, 2, 0, 1, 2, 1, 2, 0, 1, 0, 1, 2, 0,
        0, 0, 1, 1, 2, 2, 0, 0, 1, 1, 2, 2, 0, 0, 1, 1,
        0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 0, 0, 0, 0, 1, 1,
        0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2,
        0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 2, 1, 2, 1, 2, 1,
        0, 0, 2, 2, 1, 1, 2, 2, 0, 0, 2, 2, 1, 1, 2, 2,
        0, 0, 2, 2, 0, 0, 1, 1, 0, 0, 2, 2, 0, 0, 1, 1,
        0, 2, 2, 0, 1, 2, 2, 1, 0, 2, 2, 0, 1, 2, 2, 1,
        0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 0, 1, 0, 1,
        0, 0, 0, 0, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1,
        0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 2,
        0, 2, 2, 2, 0, 1, 1, 1, 0, 2, 2, 2, 0, 1, 1, 1,
        0, 0, 0, 2, 1, 1, 1, 2, 0, 0, 0, 2, 1, 1, 1, 2,
        0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2,
        0, 2, 2, 2, 0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2,
        0, 0, 0, 2, 1, 1, 1, 2, 1, 1, 1, 2, 0, 0, 0, 2,
        0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 2, 2,
        0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 1, 2,
        0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 2, 2, 2, 2, 2, 2,
        0, 0, 2, 2, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 2, 2,
        0, 0, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 0, 0, 2, 2,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2,
        0, 0, 0, 2, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 1,
        0, 2, 2, 2, 1, 2, 2, 2, 0, 2, 2, 2, 1, 2, 2, 2,
        0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        0, 1, 1, 1, 2, 0, 1, 1, 2, 2, 0, 1, 2, 2, 2, 0
    ];

    private static readonly int[] A2Table =
    [
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 2, 8, 2, 2, 8, 8, 15, 2, 8, 2, 2, 8, 8, 2, 2,
        15, 15, 6, 8, 2, 8, 15, 15, 2, 8, 2, 2, 2, 15, 15, 6,
        6, 2, 6, 8, 15, 15, 2, 2, 15, 15, 15, 15, 15, 2, 2, 15
    ];

    private static readonly int[] A3aTable =
    [
        3, 3, 15, 15, 8, 3, 15, 15, 8, 8, 6, 6, 6, 5, 3, 3,
        3, 3, 8, 15, 3, 3, 6, 10, 5, 8, 8, 6, 8, 5, 15, 15,
        8, 15, 3, 5, 6, 10, 8, 15, 15, 3, 15, 5, 15, 15, 15, 15,
        3, 15, 5, 5, 5, 8, 5, 10, 5, 10, 8, 13, 15, 12, 3, 3
    ];

    private static readonly int[] A3bTable =
    [
        15, 8, 8, 3, 15, 15, 3, 8, 15, 15, 15, 15, 15, 15, 15, 8,
        15, 8, 15, 3, 15, 8, 15, 8, 3, 15, 6, 10, 15, 15, 10, 8,
        15, 3, 15, 10, 10, 8, 9, 10, 6, 15, 8, 15, 3, 6, 6, 8,
        15, 3, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 3, 15, 15, 8
    ];

    private static readonly int[] Weight2 = [0, 21, 43, 64];
    private static readonly int[] Weight3 = [0, 9, 18, 27, 37, 46, 55, 64];
    private static readonly int[] Weight4 = [0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64];

    private static bool[] Bc7Setbits(byte[] data)
    {
        var bits = new bool[128];
        for (var i = 0; i < 16; i++)
        {
            var b = data[i];
            for (var j = 0; j < 8; j++)
            {
                bits[i * 8 + j] = (b & 1) != 0;
                b >>= 1;
            }
        }

        return bits;
    }

    private static int Bc7Getbits(bool[] bits, ref int offset, int cnt)
    {
        var intVal = 0;
        for (var i = 0; i < cnt; i++)
            if (bits[offset + i])
                intVal |= 1 << i;
        offset += cnt;
        return intVal;
    }

    private static void Bc7Rotation(ref byte[] pix, int rot)
    {
        if (rot == 0) return;
        for (var i = 0; i < pix.Length; i += 4)
        {
            var a = pix[i + 3];
            switch (rot)
            {
                case 1:
                    pix[i + 3] = pix[i + 0];
                    pix[i + 0] = a;
                    break;
                case 2:
                    pix[i + 3] = pix[i + 1];
                    pix[i + 1] = a;
                    break;
                case 3:
                    pix[i + 3] = pix[i + 2];
                    pix[i + 2] = a;
                    break;
            }
        }
    }

    private static int[][] Bc7Endpoints(bool[] bits, ref int offset, int cnt, int rgbBit, int aBit, double pBit)
    {
        var rgba = new int[cnt][];
        for (var i = 0; i < cnt; i++)
            rgba[i] = [255, 255, 255, 255];

        for (var i = 0; i < cnt; i++)
            rgba[i][0] = Bc7Getbits(bits, ref offset, rgbBit);
        for (var i = 0; i < cnt; i++)
            rgba[i][1] = Bc7Getbits(bits, ref offset, rgbBit);
        for (var i = 0; i < cnt; i++)
            rgba[i][2] = Bc7Getbits(bits, ref offset, rgbBit);

        if (aBit != 0)
            for (var i = 0; i < cnt; i++)
                rgba[i][3] = Bc7Getbits(bits, ref offset, aBit);

        var p = new List<int>();
        if (Math.Abs(pBit - 1) < 0.01)
            for (var i = 0; i < cnt; i++)
                p.Add(Bc7Getbits(bits, ref offset, 1));
        else if (Math.Abs(pBit - 0.5) < 0.01)
            for (var i = 0; i < cnt / 2; i++)
            {
                var b = Bc7Getbits(bits, ref offset, 1);
                p.Add(b);
                p.Add(b);
            }

        if (p.Count > 0)
        {
            for (var i = 0; i < cnt; i++)
            {
                rgba[i][0] = (rgba[i][0] << 1) | p[i];
                rgba[i][1] = (rgba[i][1] << 1) | p[i];
                rgba[i][2] = (rgba[i][2] << 1) | p[i];
                if (aBit != 0)
                    rgba[i][3] = (rgba[i][3] << 1) | p[i];
            }

            rgbBit++;
            if (aBit != 0)
                aBit++;
        }

        for (var i = 0; i < cnt; i++)
        {
            rgba[i][0] <<= 8 - rgbBit;
            rgba[i][1] <<= 8 - rgbBit;
            rgba[i][2] <<= 8 - rgbBit;
            rgba[i][0] |= rgba[i][0] >> rgbBit;
            rgba[i][1] |= rgba[i][1] >> rgbBit;
            rgba[i][2] |= rgba[i][2] >> rgbBit;

            if (aBit != 0)
            {
                rgba[i][3] <<= 8 - aBit;
                rgba[i][3] |= rgba[i][3] >> aBit;
            }
        }

        return rgba;
    }

    private static byte Bc7Interpolation(int e0, int e1, int index, int bit)
    {
        var w = bit switch
        {
            2 => Weight2[index],
            3 => Weight3[index],
            4 => Weight4[index],
            _ => 0
        };

        var c = (64 - w) * e0 + w * e1 + 32;
        c >>= 6;
        c = ByteHelper.IntClamp(c, 0, 255);
        return (byte)c;
    }

    private static int[] Bc7SubsetIndex(int subset, int partitionId)
    {
        var index = new int[16];
        switch (subset)
        {
            case 1:
                for (var i = 0; i < 16; i++)
                    index[i] = 0;
                break;
            case 2:
                for (var i = 0; i < 16; i++)
                    index[i] = P2Table[partitionId * 16 + i];
                break;
            case 3:
                for (var i = 0; i < 16; i++)
                    index[i] = P3Table[partitionId * 16 + i];
                break;
        }

        return index;
    }

    private static int[] Bc7AnchorIndex(int subset, int partitionId)
    {
        var index = new List<int>();
        switch (subset)
        {
            case 1:
                index.Add(0);
                break;
            case 2:
                index.Add(0);
                index.Add(A2Table[partitionId]);
                break;
            case 3:
                index.Add(0);
                index.Add(A3aTable[partitionId]);
                index.Add(A3bTable[partitionId]);
                break;
        }

        return index.ToArray();
    }

    private static byte[] Bc7PixRgba(bool[] bits, ref int offset, int[] set, int[] anch, int[][] end, int rgbBit,
        int aBit)
    {
        var pix = new byte[64];
        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            var ai = anch[si];
            var ci = i == ai ? Bc7Getbits(bits, ref offset, rgbBit - 1) : Bc7Getbits(bits, ref offset, rgbBit);

            var e0 = end[si * 2 + 0];
            var e1 = end[si * 2 + 1];
            pix[i * 4 + 0] = Bc7Interpolation(e0[0], e1[0], ci, rgbBit);
            pix[i * 4 + 1] = Bc7Interpolation(e0[1], e1[1], ci, rgbBit);
            pix[i * 4 + 2] = Bc7Interpolation(e0[2], e1[2], ci, rgbBit);
            if (aBit == 0)
                pix[i * 4 + 3] = 255;
            else
                pix[i * 4 + 3] = Bc7Interpolation(e0[3], e1[3], ci, aBit);
        }

        return pix;
    }

    private static byte[] Bc7Mode0(bool[] bits, ref int offset)
    {
        var sub = 3;
        var part = Bc7Getbits(bits, ref offset, 4);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 4, 0, 1);
        var set = Bc7SubsetIndex(sub, part);
        var anch = Bc7AnchorIndex(sub, part);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 3, 0);
    }

    private static byte[] Bc7Mode1(bool[] bits, ref int offset)
    {
        var sub = 2;
        var part = Bc7Getbits(bits, ref offset, 6);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 6, 0, 0.5);
        var set = Bc7SubsetIndex(sub, part);
        var anch = Bc7AnchorIndex(sub, part);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 3, 0);
    }

    private static byte[] Bc7Mode2(bool[] bits, ref int offset)
    {
        var sub = 3;
        var part = Bc7Getbits(bits, ref offset, 6);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 5, 0, 0);
        var set = Bc7SubsetIndex(sub, part);
        var anch = Bc7AnchorIndex(sub, part);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 2, 0);
    }

    private static byte[] Bc7Mode3(bool[] bits, ref int offset)
    {
        var sub = 2;
        var part = Bc7Getbits(bits, ref offset, 6);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 7, 0, 1);
        var set = Bc7SubsetIndex(sub, part);
        var anch = Bc7AnchorIndex(sub, part);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 2, 0);
    }

    private static byte[] Bc7Mode4(bool[] bits, ref int offset)
    {
        const int sub = 1;
        var rot = Bc7Getbits(bits, ref offset, 2);
        var idx = Bc7Getbits(bits, ref offset, 1);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 5, 6, 0);
        var set = Bc7SubsetIndex(sub, 0);
        var anch = Bc7AnchorIndex(sub, 0);

        var cb = idx != 0 ? 3 : 2;
        var ab = idx != 0 ? 2 : 3;
        var ci = new int[16];
        var ai = new int[16];

        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            if (i == anch[si])
            {
                if (idx != 0)
                    ai[i] = Bc7Getbits(bits, ref offset, ab - 1);
                else
                    ci[i] = Bc7Getbits(bits, ref offset, cb - 1);
            }
            else
            {
                if (idx != 0)
                    ai[i] = Bc7Getbits(bits, ref offset, ab);
                else
                    ci[i] = Bc7Getbits(bits, ref offset, cb);
            }
        }

        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            if (i == anch[si])
            {
                if (idx != 0)
                    ci[i] = Bc7Getbits(bits, ref offset, cb - 1);
                else
                    ai[i] = Bc7Getbits(bits, ref offset, ab - 1);
            }
            else
            {
                if (idx != 0)
                    ci[i] = Bc7Getbits(bits, ref offset, cb);
                else
                    ai[i] = Bc7Getbits(bits, ref offset, ab);
            }
        }

        var pix = new byte[64];
        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            var e0 = end[si * 2 + 0];
            var e1 = end[si * 2 + 1];
            pix[i * 4 + 0] = Bc7Interpolation(e0[0], e1[0], ci[i], cb);
            pix[i * 4 + 1] = Bc7Interpolation(e0[1], e1[1], ci[i], cb);
            pix[i * 4 + 2] = Bc7Interpolation(e0[2], e1[2], ci[i], cb);
            pix[i * 4 + 3] = Bc7Interpolation(e0[3], e1[3], ai[i], ab);
        }

        Bc7Rotation(ref pix, rot);
        return pix;
    }

    private static byte[] Bc7Mode5(bool[] bits, ref int offset)
    {
        const int sub = 1;
        var rot = Bc7Getbits(bits, ref offset, 2);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 7, 8, 0);
        var set = Bc7SubsetIndex(sub, 0);
        var anch = Bc7AnchorIndex(sub, 0);

        var ci = new int[16];
        var ai = new int[16];

        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            if (i == anch[si])
                ci[i] = Bc7Getbits(bits, ref offset, 1);
            else
                ci[i] = Bc7Getbits(bits, ref offset, 2);
        }

        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            if (i == anch[si])
                ai[i] = Bc7Getbits(bits, ref offset, 1);
            else
                ai[i] = Bc7Getbits(bits, ref offset, 2);
        }

        var pix = new byte[64];
        for (var i = 0; i < 16; i++)
        {
            var si = set[i];
            var e0 = end[si * 2 + 0];
            var e1 = end[si * 2 + 1];
            pix[i * 4 + 0] = Bc7Interpolation(e0[0], e1[0], ci[i], 2);
            pix[i * 4 + 1] = Bc7Interpolation(e0[1], e1[1], ci[i], 2);
            pix[i * 4 + 2] = Bc7Interpolation(e0[2], e1[2], ci[i], 2);
            pix[i * 4 + 3] = Bc7Interpolation(e0[3], e1[3], ai[i], 2);
        }

        Bc7Rotation(ref pix, rot);
        return pix;
    }

    private static byte[] Bc7Mode6(bool[] bits, ref int offset)
    {
        const int sub = 1;
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 7, 7, 1);
        var set = Bc7SubsetIndex(sub, 0);
        var anch = Bc7AnchorIndex(sub, 0);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 4, 4);
    }

    private static byte[] Bc7Mode7(bool[] bits, ref int offset)
    {
        const int sub = 2;
        var part = Bc7Getbits(bits, ref offset, 6);
        var end = Bc7Endpoints(bits, ref offset, sub * 2, 5, 5, 1);
        var set = Bc7SubsetIndex(sub, part);
        var anch = Bc7AnchorIndex(sub, part);
        return Bc7PixRgba(bits, ref offset, set, anch, end, 2, 2);
    }

    private static byte[] Bc7Block(bool[] bits)
    {
        var offset = 0;
        if (bits[offset++]) return Bc7Mode0(bits, ref offset);
        if (bits[offset++]) return Bc7Mode1(bits, ref offset);
        if (bits[offset++]) return Bc7Mode2(bits, ref offset);
        if (bits[offset++]) return Bc7Mode3(bits, ref offset);
        if (bits[offset++]) return Bc7Mode4(bits, ref offset);
        if (bits[offset++]) return Bc7Mode5(bits, ref offset);
        if (bits[offset++]) return Bc7Mode6(bits, ref offset);
        if (bits[offset++]) return Bc7Mode7(bits, ref offset);
        return new byte[64];
    }

    public byte[] Bc7(byte[] data)
    {
        var pix = new List<byte>();
        var len = ByteHelper.IntCeil(data.Length, 16);
        for (var i = 0; i < len; i += 16)
        {
            var blockData = new byte[16];
            Array.Copy(data, i, blockData, 0, Math.Min(16, data.Length - i));
            var bits = Bc7Setbits(blockData);
            pix.AddRange(Bc7Block(bits));
        }

        return pix.ToArray();
    }

    public byte[] Bptc(byte[] data)
    {
        return Bc7(data);
    }
}