namespace VanillawareConverter.Ftex.Swizzling;

public static class Ps2Swizzle
{
    public static byte[] Unswizz8(byte[] pix, int w, int h)
    {
        var dec = new byte[w * h];
        var pos = 0;

        for (var y = 0; y < h; y += 16)
        for (var x = 0; x < w; x += 16)
        for (var dy = 0; dy < 16; dy++)
        for (var dx = 0; dx < 16; dx++)
        {
            var srcY = y + dy;
            var srcX = x + dx;
            if (srcY < h && srcX < w && pos < pix.Length)
                dec[srcY * w + srcX] = pix[pos++];
            else if (pos < pix.Length) pos++;
        }

        return dec;
    }

    public static byte[] Unswizz4(byte[] pix, int w, int h)
    {
        var dec = new byte[w * h];
        var pos = 0;

        for (var y = 0; y < h; y += 16)
        for (var x = 0; x < w; x += 32)
        for (var dy = 0; dy < 16; dy++)
        for (var dx = 0; dx < 32; dx++)
        {
            var srcY = y + dy;
            var srcX = x + dx;
            if (srcY < h && srcX < w && pos < pix.Length)
                dec[srcY * w + srcX] = pix[pos++];
            else if (pos < pix.Length) pos++;
        }

        return dec;
    }
}