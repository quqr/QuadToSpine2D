using System.Text;
using VanillawareConverter.Common;
using VanillawareConverter.Ftex.Swizzling;

namespace VanillawareConverter.Ftex.Parsers;

public class Ps2FtexParser : BaseFtexParser
{
    public override GamePlatform Platform => GamePlatform.PS2;

    protected override int MinimumFileLength => 0x30;

    protected override bool CheckMagic(byte[] fileData)
    {
        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        if (magic != "FTEX")
            return false;

        var st = 0x20;
        if (st + 0x10 > fileData.Length)
            return false;

        var ftxMagic = Encoding.ASCII.GetString(fileData, st, 4);
        if (ftxMagic != "FTX0")
            return false;

        var sz2 = (int)ByteHelper.ReadUInt32(fileData, st + 8);
        if (st + sz2 + 4 > fileData.Length)
            return false;

        var texMagic = Encoding.ASCII.GetString(fileData, st + sz2, 4);
        return texMagic == "FGST";
    }

    protected override void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results)
    {
        var hdsz = (int)ByteHelper.ReadUInt32(fileData, 8);
        var cnt = (int)ByteHelper.ReadUInt32(fileData, 12);

        var st = hdsz;
        for (var i = 0; i < cnt; i++)
        {
            var p1 = 0x20 + i * 0x30;
            if (p1 + 0x20 > fileData.Length)
                break;

            var fnBytes = new byte[0x20];
            Array.Copy(fileData, p1, fnBytes, 0, 0x20);
            fnBytes = ByteHelper.RTrim(fnBytes, 0);

            if (st + 4 > fileData.Length)
                break;

            var ftxMagic = Encoding.ASCII.GetString(fileData, st, 4);
            if (ftxMagic != "FTX0")
                break;

            var sz1 = (int)ByteHelper.ReadUInt32(fileData, st + 4);
            var sz2 = (int)ByteHelper.ReadUInt32(fileData, st + 8);

            var result = ParseFgst(fileData, st + sz2);
            if (result != null) results.Add(result);

            st += sz1 + sz2;
        }
    }

    private ImageResult? ParseFgst(byte[] fileData, int offset)
    {
        if (offset + 0x40 > fileData.Length)
            return null;

        var fgst = Encoding.ASCII.GetString(fileData, offset, 4);
        if (fgst != "FGST")
            return null;

        var w = (int)ByteHelper.ReadUInt32(fileData, offset + 0x10);
        var h = (int)ByteHelper.ReadUInt32(fileData, offset + 0x14);
        var fmt = (int)ByteHelper.ReadUInt32(fileData, offset + 0x18);
        var pxt = (int)ByteHelper.ReadUInt32(fileData, offset + 0x1C);
        var pws = (int)ByteHelper.ReadUInt32(fileData, offset + 0x20);
        var phs = (int)ByteHelper.ReadUInt32(fileData, offset + 0x24);
        var clutPos = (int)ByteHelper.ReadUInt32(fileData, offset + 0x28);
        var clutCnt = (int)ByteHelper.ReadUInt32(fileData, offset + 0x2C);
        var pixPos = (int)ByteHelper.ReadUInt32(fileData, offset + 0x30);
        var pixCnt = (int)ByteHelper.ReadUInt32(fileData, offset + 0x34);

        byte[]? palette = null;
        var colorCount = 0;

        if (clutCnt > 0 && clutPos + clutCnt * 4 <= fileData.Length)
        {
            colorCount = clutCnt;
            palette = new byte[clutCnt * 4];
            for (var i = 0; i < clutCnt; i++)
            {
                var p = offset + clutPos + i * 4;
                palette[i * 4 + 0] = fileData[p + 0];
                palette[i * 4 + 1] = fileData[p + 1];
                palette[i * 4 + 2] = fileData[p + 2];
                palette[i * 4 + 3] = (byte)(fileData[p + 3] * 2);
                if (palette[i * 4 + 3] > 255)
                    palette[i * 4 + 3] = 255;
            }
        }

        byte[]? pixelData = null;

        if (pixPos > 0 && pixCnt > 0 && offset + pixPos + pixCnt <= fileData.Length)
        {
            pixelData = new byte[pixCnt];
            Array.Copy(fileData, offset + pixPos, pixelData, 0, pixCnt);
        }

        if (pixelData == null)
            return null;

        switch (fmt)
        {
            case 0:
                return ProcessRgba32(pixelData, w, h, pxt, pws, phs);
            case 1:
                return ProcessIndexed8(pixelData, palette, colorCount, w, h, pxt, pws, phs);
            case 2:
                return ProcessIndexed4(pixelData, palette, colorCount, w, h, pxt, pws, phs);
            default:
                return null;
        }
    }

    private ImageResult ProcessRgba32(byte[] pix, int w, int h, int pxt, int pws, int phs)
    {
        if (pxt != 0) pix = Ps2Swizzle.Unswizz8(pix, w * 4, h);

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

    private ImageResult ProcessIndexed8(byte[] pix, byte[]? palette, int colorCount, int w, int h, int pxt, int pws,
        int phs)
    {
        if (pxt != 0) pix = Ps2Swizzle.Unswizz8(pix, w, h);

        return new ImageResult(w, h, pix)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }

    private ImageResult ProcessIndexed4(byte[] pix, byte[]? palette, int colorCount, int w, int h, int pxt, int pws,
        int phs)
    {
        if (pxt != 0) pix = Ps2Swizzle.Unswizz4(pix, w, h);

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