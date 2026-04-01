using System.Text;
using VanillawareConverter.Ftex.Swizzling;

namespace VanillawareConverter.Ftex.Parsers;

public class PspFtexParser : IFtexParser
{
    public GamePlatform Platform => GamePlatform.PSP;

    public bool CanParse(byte[] fileData)
    {
        if (fileData == null || fileData.Length < 0x10)
            return false;

        if (fileData.Length < 16)
            return false;

        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        if (magic != "MIG.")
            return false;

        if (fileData.Length < 0x50)
            return false;

        var marker = Encoding.ASCII.GetString(fileData, 0x40, 8);
        return marker == ".00.1PSP";
    }

    public List<ImageResult> Parse(byte[] fileData, string outputPrefix)
    {
        var results = new List<ImageResult>();

        if (!CanParse(fileData))
            return results;

        var offset = 0x100;
        while (offset + 0x40 < fileData.Length)
        {
            if (fileData[offset] == 0 && fileData[offset + 1] == 0)
                break;

            var pixPos = (int)ByteHelper.ReadUInt32(fileData, offset + 0x00);
            var palPos = (int)ByteHelper.ReadUInt32(fileData, offset + 0x04);
            var w = (int)ByteHelper.ReadUInt16(fileData, offset + 0x08);
            var h = (int)ByteHelper.ReadUInt16(fileData, offset + 0x0A);
            int fmt = fileData[offset + 0x0C];
            int swizzle = fileData[offset + 0x0D];

            if (pixPos == 0 || w == 0 || h == 0)
                break;

            byte[]? palette = null;
            var colorCount = 0;

            if (palPos > 0 && palPos + 0x400 <= fileData.Length)
            {
                colorCount = 256;
                palette = new byte[256 * 4];
                for (var i = 0; i < 256; i++)
                {
                    var p = palPos + i * 4;
                    palette[i * 4 + 0] = fileData[p + 0];
                    palette[i * 4 + 1] = fileData[p + 1];
                    palette[i * 4 + 2] = fileData[p + 2];
                    palette[i * 4 + 3] = (byte)(fileData[p + 3] * 2);
                    if (palette[i * 4 + 3] > 255)
                        palette[i * 4 + 3] = 255;
                }
            }

            var result = ProcessTexture(fileData, pixPos, w, h, fmt, swizzle, palette, colorCount);
            if (result != null) results.Add(result);

            offset += 0x40;
        }

        return results;
    }

    private ImageResult? ProcessTexture(byte[] fileData, int pixPos, int w, int h, int fmt, int swizzle,
        byte[]? palette, int colorCount)
    {
        switch (fmt)
        {
            case 3:
                return ProcessRgba32(fileData, pixPos, w, h, swizzle);
            case 4:
                return ProcessIndexed4(fileData, pixPos, palette, colorCount, w, h, swizzle);
            case 5:
                return ProcessIndexed8(fileData, pixPos, palette, colorCount, w, h, swizzle);
            default:
                return null;
        }
    }

    private ImageResult ProcessRgba32(byte[] fileData, int pixPos, int w, int h, int swizzle)
    {
        var dataSize = w * h * 4;
        if (pixPos + dataSize > fileData.Length)
            return new ImageResult(w, h, []);

        var pix = new byte[dataSize];
        Array.Copy(fileData, pixPos, pix, 0, dataSize);

        if (swizzle != 0) pix = PspSwizzle.Gimpix(pix, w, h, 4);

        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4 + 0] = pix[i * 4 + 0];
            rgba[i * 4 + 1] = pix[i * 4 + 1];
            rgba[i * 4 + 2] = pix[i * 4 + 2];
            rgba[i * 4 + 3] = (byte)(pix[i * 4 + 3] * 2);
            if (rgba[i * 4 + 3] > 255)
                rgba[i * 4 + 3] = 255;
        }

        return new ImageResult(w, h, rgba);
    }

    private ImageResult ProcessIndexed4(byte[] fileData, int pixPos, byte[]? palette, int colorCount, int w, int h,
        int swizzle)
    {
        var dataSize = (w * h + 1) / 2;
        if (pixPos + dataSize > fileData.Length)
            return new ImageResult(w, h, []);

        var pix = new byte[dataSize];
        Array.Copy(fileData, pixPos, pix, 0, dataSize);

        var expanded = new byte[w * h];
        for (var i = 0; i < dataSize && i * 2 < expanded.Length; i++)
        {
            var b = pix[i];
            expanded[i * 2 + 0] = (byte)(b & 0x0F);
            expanded[i * 2 + 1] = (byte)((b >> 4) & 0x0F);
        }

        if (swizzle != 0) expanded = PspSwizzle.Gimpix(expanded, w, h, 1);

        return new ImageResult(w, h, expanded)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }

    private ImageResult ProcessIndexed8(byte[] fileData, int pixPos, byte[]? palette, int colorCount, int w, int h,
        int swizzle)
    {
        var dataSize = w * h;
        if (pixPos + dataSize > fileData.Length)
            return new ImageResult(w, h, []);

        var pix = new byte[dataSize];
        Array.Copy(fileData, pixPos, pix, 0, dataSize);

        if (swizzle != 0) pix = PspSwizzle.Gimpix(pix, w, h, 1);

        return new ImageResult(w, h, pix)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }
}