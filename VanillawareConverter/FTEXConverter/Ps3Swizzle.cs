namespace VanillawareConverter.Ftex.Swizzling;

public static class Ps3Swizzle
{
    public static byte[] ArgbSwizzled(byte[] pix, int w, int h)
    {
        var dec = new byte[w * h * 4];
        var pos = 0;

        var bw = (w + 7) / 8;
        var bh = (h + 7) / 8;

        for (var by = 0; by < bh; by++)
        for (var bx = 0; bx < bw; bx++)
        for (var y = 0; y < 8; y++)
        for (var x = 0; x < 8; x++)
        {
            var dstX = bx * 8 + x;
            var dstY = by * 8 + y;

            if (dstX < w && dstY < h && pos + 4 <= pix.Length)
            {
                var dstPos = (dstY * w + dstX) * 4;
                dec[dstPos + 0] = pix[pos + 0];
                dec[dstPos + 1] = pix[pos + 1];
                dec[dstPos + 2] = pix[pos + 2];
                dec[dstPos + 3] = pix[pos + 3];
            }

            pos += 4;
        }

        return dec;
    }
}