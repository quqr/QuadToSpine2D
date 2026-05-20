namespace VanillawareConverter.Ftex.Swizzling;

public static class PspSwizzle
{
    public static byte[] Gimpix(byte[] pix, int w, int h, int bpp)
    {
        var pixelCount = w * h;
        var dec = new byte[pixelCount * bpp];

        var wx = new int[16] { 0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 };
        var wy = new int[8] { 0, 2, 8, 10, 4, 6, 12, 14 };

        var pos = 0;
        for (var by = 0; by < h; by += 8)
        for (var bx = 0; bx < w; bx += 16)
        for (var i = 0; i < 16; i++)
        for (var j = 0; j < 8; j++)
        {
            var x = bx + wx[i];
            var y = by + wy[j];

            if (x < w && y < h)
            {
                var dstPos = (y * w + x) * bpp;
                for (var b = 0; b < bpp && pos < pix.Length; b++)
                    if (dstPos + b < dec.Length)
                        dec[dstPos + b] = pix[pos++];
                    else
                        pos++;
            }
            else
            {
                pos += bpp;
            }
        }

        return dec;
    }
}