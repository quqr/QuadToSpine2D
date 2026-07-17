using VanillawareConverter.Common;
using VanillawareConverter.Ftex.Swizzling;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex.Parsers;

public class WiiFtexParser : BaseFtexParser
{
    private readonly S3tcTexture _s3tc = new();

    public override GamePlatform Platform => GamePlatform.Wii;

    protected override int MinimumFileLength => 4;

    protected override bool CheckMagic(byte[] fileData)
    {
        var magic = BitConverter.ToUInt32(fileData, 0);
        return magic == 0x20af30;
    }

    protected override void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results)
    {
        var texCount = (int)ByteHelper.ReadUInt32(fileData, 0x08);
        var texOffset = (int)ByteHelper.ReadUInt32(fileData, 0x0C);
        var palOffset = (int)ByteHelper.ReadUInt32(fileData, 0x10);

        for (var i = 0; i < texCount; i++)
        {
            var texEntry = texOffset + i * 0x10;
            if (texEntry + 0x10 > fileData.Length)
                break;

            var w = (int)ByteHelper.ReadUInt16(fileData, texEntry + 0x00);
            var h = (int)ByteHelper.ReadUInt16(fileData, texEntry + 0x02);
            int fmt = fileData[texEntry + 0x04];
            int palIdx = fileData[texEntry + 0x05];
            var dataOffset = (int)ByteHelper.ReadUInt32(fileData, texEntry + 0x08);
            var dataSize = (int)ByteHelper.ReadUInt32(fileData, texEntry + 0x0C);

            if (dataOffset == 0 || w == 0 || h == 0)
                continue;

            byte[]? palette = null;
            var colorCount = 0;

            if (palOffset > 0 && palIdx < 0x100)
            {
                var palEntry = palOffset + palIdx * 0x08;
                if (palEntry + 0x08 <= fileData.Length)
                {
                    var palDataOffset = (int)ByteHelper.ReadUInt32(fileData, palEntry + 0x00);
                    var palDataSize = (int)ByteHelper.ReadUInt32(fileData, palEntry + 0x04);
                    int palFmt = fileData[palEntry + 0x06];

                    if (palDataOffset > 0 && palDataOffset + palDataSize <= fileData.Length)
                        palette = ParsePalette(fileData, palDataOffset, palDataSize, palFmt, out colorCount);
                }
            }

            if (dataOffset + dataSize > fileData.Length)
                continue;

            var pixelData = new byte[dataSize];
            Array.Copy(fileData, dataOffset, pixelData, 0, dataSize);

            var result = ProcessTexture(pixelData, palette, colorCount, w, h, fmt);
            if (result != null) results.Add(result);
        }
    }

    private byte[] ParsePalette(byte[] fileData, int offset, int size, int fmt, out int colorCount)
    {
        colorCount = size / 2;
        var palette = new byte[colorCount * 4];

        for (var i = 0; i < colorCount && offset + i * 2 + 1 < fileData.Length; i++)
        {
            var color = ByteHelper.ReadUInt16(fileData, offset + i * 2);

            switch (fmt)
            {
                case 0:
                    palette[i * 4 + 0] = (byte)(color >> 8);
                    palette[i * 4 + 1] = (byte)(color >> 8);
                    palette[i * 4 + 2] = (byte)(color >> 8);
                    palette[i * 4 + 3] = (byte)(color & 0xFF);
                    break;
                case 1:
                {
                    var r = ((color >> 11) & 0x1F) << 3;
                    var g = ((color >> 5) & 0x3F) << 2;
                    var b = (color & 0x1F) << 3;
                    palette[i * 4 + 0] = (byte)r;
                    palette[i * 4 + 1] = (byte)g;
                    palette[i * 4 + 2] = (byte)b;
                    palette[i * 4 + 3] = 255;
                }
                    break;
                case 2:
                {
                    int r, g, b, a;
                    if ((color & 0x8000) != 0)
                    {
                        r = ((color >> 10) & 0x1F) << 3;
                        g = ((color >> 5) & 0x1F) << 3;
                        b = (color & 0x1F) << 3;
                        a = 255;
                    }
                    else
                    {
                        a = ((color >> 12) & 0x07) << 5;
                        r = ((color >> 8) & 0x0F) << 4;
                        g = ((color >> 4) & 0x0F) << 4;
                        b = (color & 0x0F) << 4;
                    }

                    palette[i * 4 + 0] = (byte)r;
                    palette[i * 4 + 1] = (byte)g;
                    palette[i * 4 + 2] = (byte)b;
                    palette[i * 4 + 3] = (byte)a;
                }
                    break;
                default:
                    palette[i * 4 + 0] = 0;
                    palette[i * 4 + 1] = 0;
                    palette[i * 4 + 2] = 0;
                    palette[i * 4 + 3] = 255;
                    break;
            }
        }

        return palette;
    }

    private ImageResult? ProcessTexture(byte[] data, byte[]? palette, int colorCount, int w, int h, int fmt)
    {
        switch (fmt)
        {
            case 0:
                return ProcessRgba32(data, w, h);
            case 1:
                return ProcessC4(data, palette, colorCount, w, h);
            case 2:
                return ProcessC8(data, palette, colorCount, w, h);
            case 3:
                return ProcessC14X2(data, palette, colorCount, w, h);
            case 4:
                return ProcessCmpr(data, w, h);
            default:
                return null;
        }
    }

    private ImageResult ProcessRgba32(byte[] data, int w, int h)
    {
        var rgba = WiiSwizzle.TplImage(data, w, h, 4);
        return new ImageResult(w, h, rgba);
    }

    private ImageResult ProcessC4(byte[] data, byte[]? palette, int colorCount, int w, int h)
    {
        var expanded = new byte[w * h];
        var pos = 0;

        for (var by = 0; by < (h + 7) / 8; by++)
        for (var bx = 0; bx < (w + 7) / 8; bx++)
        for (var y = 0; y < 8; y++)
        for (var x = 0; x < 8; x += 2)
        {
            if (pos >= data.Length)
                break;

            var b = data[pos++];
            var dstX = bx * 8 + x;
            var dstY = by * 8 + y;

            if (dstX + 0 < w && dstY < h)
                expanded[dstY * w + dstX + 0] = (byte)(b >> 4);
            if (dstX + 1 < w && dstY < h)
                expanded[dstY * w + dstX + 1] = (byte)(b & 0x0F);
        }

        return new ImageResult(w, h, expanded)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }

    private ImageResult ProcessC8(byte[] data, byte[]? palette, int colorCount, int w, int h)
    {
        var expanded = WiiSwizzle.TplImage(data, w, h, 1);
        return new ImageResult(w, h, expanded)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }

    private ImageResult ProcessC14X2(byte[] data, byte[]? palette, int colorCount, int w, int h)
    {
        var expanded = new byte[w * h];
        var pos = 0;

        for (var by = 0; by < (h + 3) / 4; by++)
        for (var bx = 0; bx < (w + 3) / 4; bx++)
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            if (pos + 1 >= data.Length)
                break;

            var idx = ByteHelper.ReadUInt16(data, pos);
            pos += 2;

            var dstX = bx * 4 + x;
            var dstY = by * 4 + y;

            if (dstX < w && dstY < h) expanded[dstY * w + dstX] = (byte)(idx & 0x3FFF);
        }

        return new ImageResult(w, h, expanded)
        {
            Palette = palette,
            ColorCount = colorCount
        };
    }

    private ImageResult ProcessCmpr(byte[] data, int w, int h)
    {
        var blockW = (w + 7) / 8;
        var blockH = (h + 7) / 8;

        var rgba = new byte[w * h * 4];
        var pos = 0;

        for (var by = 0; by < blockH; by++)
        for (var bx = 0; bx < blockW; bx++)
        for (var subY = 0; subY < 2; subY++)
        for (var subX = 0; subX < 2; subX++)
        {
            if (pos + 8 > data.Length)
                break;

            var blockData = new byte[8];
            Array.Copy(data, pos, blockData, 0, 8);
            pos += 8;

            var blockRgba = DecodeDxt1Block(blockData);

            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var dstX = bx * 8 + subX * 4 + x;
                var dstY = by * 8 + subY * 4 + y;

                if (dstX < w && dstY < h)
                {
                    var dstPos = (dstY * w + dstX) * 4;
                    var srcPos = (y * 4 + x) * 4;
                    rgba[dstPos + 0] = blockRgba[srcPos + 0];
                    rgba[dstPos + 1] = blockRgba[srcPos + 1];
                    rgba[dstPos + 2] = blockRgba[srcPos + 2];
                    rgba[dstPos + 3] = blockRgba[srcPos + 3];
                }
            }
        }

        return new ImageResult(w, h, rgba);
    }

    private byte[] DecodeDxt1Block(byte[] data)
    {
        var c0 = ByteHelper.ReadUInt16(data, 0);
        var c1 = ByteHelper.ReadUInt16(data, 2);

        var colors = new byte[4 * 4];
        colors[0] = (byte)((c0 >> 11) << 3);
        colors[1] = (byte)(((c0 >> 5) & 0x3F) << 2);
        colors[2] = (byte)((c0 & 0x1F) << 3);
        colors[3] = 255;

        colors[4] = (byte)((c1 >> 11) << 3);
        colors[5] = (byte)(((c1 >> 5) & 0x3F) << 2);
        colors[6] = (byte)((c1 & 0x1F) << 3);
        colors[7] = 255;

        if (c0 > c1)
        {
            for (var i = 0; i < 3; i++)
            {
                colors[8 + i] = (byte)((2 * colors[0 + i] + colors[4 + i]) / 3);
                colors[12 + i] = (byte)((colors[0 + i] + 2 * colors[4 + i]) / 3);
            }

            colors[11] = 255;
            colors[15] = 255;
        }
        else
        {
            for (var i = 0; i < 3; i++) colors[8 + i] = (byte)((colors[0 + i] + colors[4 + i]) / 2);
            colors[11] = 255;
            colors[12] = 0;
            colors[13] = 0;
            colors[14] = 0;
            colors[15] = 0;
        }

        var rgba = new byte[16 * 4];
        var indices = BitConverter.ToUInt32(data, 4);

        for (var i = 0; i < 16; i++)
        {
            var idx = (int)((indices >> (i * 2)) & 3);
            rgba[i * 4 + 0] = colors[idx * 4 + 0];
            rgba[i * 4 + 1] = colors[idx * 4 + 1];
            rgba[i * 4 + 2] = colors[idx * 4 + 2];
            rgba[i * 4 + 3] = colors[idx * 4 + 3];
        }

        return rgba;
    }
}