using System.Text;

namespace VanillawareConverter.Ftex.Parsers;

public class NdsFtexParser : IFtexParser
{
    public GamePlatform Platform => GamePlatform.NDS;

    public bool CanParse(byte[]? fileData)
    {
        if (fileData == null || fileData.Length < 8)
            return false;

        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        return magic is "BIT\0" or "BITD";
    }

    public List<ImageResult> Parse(byte[] fileData, string outputPrefix)
    {
        var results = new List<ImageResult>();

        if (!CanParse(fileData))
            return results;

        byte[]? palette = null;
        var colorCount = 0;

        var offset = 0;
        while (offset + 8 < fileData.Length)
        {
            var chunkMagic = Encoding.ASCII.GetString(fileData, offset, 4);
            var chunkSize = (int)ByteHelper.ReadUInt32(fileData, offset + 4);

            if (chunkMagic is "PAL\0" or "PALT")
            {
                palette = ParsePaletteData(fileData, offset + 8, chunkSize - 8, out colorCount);
            }
            else if (chunkMagic is "BIT\0" or "BITD")
            {
                var result = ParseBitmap(fileData, offset, palette, colorCount);
                if (result != null) results.Add(result);
            }

            if (chunkSize <= 0)
                break;

            offset += chunkSize;
        }

        return results;
    }

    private byte[] ParsePaletteData(byte[] fileData, int offset, int size, out int colorCount)
    {
        colorCount = size / 2;
        var palette = new byte[colorCount * 4];

        for (var i = 0; i < colorCount && offset + i * 2 + 1 < fileData.Length; i++)
        {
            var color = ByteHelper.ReadUInt16(fileData, offset + i * 2);
            var r = (color & 0x1F) << 3;
            var g = ((color >> 5) & 0x1F) << 3;
            var b = ((color >> 10) & 0x1F) << 3;

            palette[i * 4 + 0] = (byte)r;
            palette[i * 4 + 1] = (byte)g;
            palette[i * 4 + 2] = (byte)b;
            palette[i * 4 + 3] = 255;
        }

        return palette;
    }

    private ImageResult? ParseBitmap(byte[] fileData, int offset, byte[]? palette, int colorCount)
    {
        if (offset + 0x20 > fileData.Length)
            return null;

        var chunkSize = (int)ByteHelper.ReadUInt32(fileData, offset + 4);
        var w = (int)ByteHelper.ReadUInt16(fileData, offset + 8);
        var h = (int)ByteHelper.ReadUInt16(fileData, offset + 10);
        int fmt = fileData[offset + 12];

        var dataSize = chunkSize - 0x20;
        if (offset + chunkSize > fileData.Length || dataSize <= 0)
            return null;

        var pixelData = new byte[dataSize];
        Array.Copy(fileData, offset + 0x20, pixelData, 0, dataSize);

        return ProcessIndexed4(pixelData, palette, colorCount, w, h);
    }

    private ImageResult ProcessIndexed4(byte[] pix, byte[]? palette, int colorCount, int w, int h)
    {
        var expanded = new byte[w * h];
        for (var i = 0; i < pix.Length && i * 2 < expanded.Length; i++)
        {
            var b = pix[i];
            expanded[i * 2 + 0] = (byte)(b & 0x0F);
            expanded[i * 2 + 1] = (byte)((b >> 4) & 0x0F);
        }

        return new ImageResult(w, h, expanded)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }
}