namespace VanillawareConverter.Ftex.Swizzling;

public static class WiiSwizzle
{
    public static byte[] TplImage(byte[] pix, int w, int h, int bpp)
    {
        var pixelCount = w * h;
        var dec = new byte[pixelCount * bpp];

        var bw = (w + 3) / 4;
        var bh = (h + 3) / 4;

        var pos = 0;
        for (var by = 0; by < bh; by++)
        for (var bx = 0; bx < bw; bx++)
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            var dstX = bx * 4 + x;
            var dstY = by * 4 + y;

            if (dstX < w && dstY < h)
            {
                var dstPos = (dstY * w + dstX) * bpp;
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