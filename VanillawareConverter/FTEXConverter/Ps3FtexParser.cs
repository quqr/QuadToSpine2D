using System.Text;
using VanillawareConverter.Common;
using VanillawareConverter.Ftex.Swizzling;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex.Parsers;

public class Ps3FtexParser : BaseFtexParser
{
    private readonly S3tcTexture _s3tc = new();

    public override GamePlatform Platform => GamePlatform.PS3;

    protected override int MinimumFileLength => 0x10;

    protected override bool CheckMagic(byte[] fileData)
    {
        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        return magic is "gtf\0" or "gtf";
    }

    protected override void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results)
    {
        var offset = 0;
        while (offset + 0x30 < fileData.Length)
        {
            var magic = Encoding.ASCII.GetString(fileData, offset, 3);
            if (magic != "gtf")
                break;

            var w = (int)ByteHelper.ReadUInt32(fileData, offset + 0x08);
            var h = (int)ByteHelper.ReadUInt32(fileData, offset + 0x0C);
            var fmt = (int)ByteHelper.ReadUInt32(fileData, offset + 0x10);
            var swizzle = (int)ByteHelper.ReadUInt32(fileData, offset + 0x14);
            var mipCount = (int)ByteHelper.ReadUInt32(fileData, offset + 0x18);
            var dataSize = (int)ByteHelper.ReadUInt32(fileData, offset + 0x1C);
            var dataOffset = (int)ByteHelper.ReadUInt32(fileData, offset + 0x20);

            if (dataOffset + dataSize > fileData.Length)
                break;

            var pixelData = new byte[dataSize];
            Array.Copy(fileData, dataOffset, pixelData, 0, dataSize);

            var result = ProcessTexture(pixelData, w, h, fmt, swizzle);
            if (result != null) results.Add(result);

            offset = dataOffset + dataSize;
        }
    }

    private ImageResult? ProcessTexture(byte[] data, int w, int h, int fmt, int swizzle)
    {
        byte[] rgba;

        switch (fmt)
        {
            case 0x81:
            case 0x82:
                rgba = _s3tc.Dxt1(data);
                break;
            case 0x83:
            case 0x84:
                rgba = _s3tc.Dxt3(data);
                break;
            case 0x85:
            case 0x86:
                rgba = _s3tc.Dxt5(data);
                break;
            case 0x9A:
                rgba = data;
                if (swizzle != 0) rgba = Ps3Swizzle.ArgbSwizzled(rgba, w, h);
                break;
            default:
                return null;
        }

        if (fmt != 0x9A) rgba = DeswizzleDxt(rgba, w, h);

        return new ImageResult(w, h, rgba);
    }

    private byte[] DeswizzleDxt(byte[] data, int w, int h)
    {
        var blockW = (w + 3) / 4;
        var blockH = (h + 3) / 4;
        var result = new byte[blockW * blockH * 64];

        for (var by = 0; by < blockH; by++)
        for (var bx = 0; bx < blockW; bx++)
        {
            var srcIdx = by * blockW + bx;
            var dstIdx = bx * blockH + by;

            if (srcIdx * 64 + 64 <= data.Length && dstIdx * 64 + 64 <= result.Length)
                Array.Copy(data, srcIdx * 64, result, dstIdx * 64, 64);
        }

        return result;
    }
}