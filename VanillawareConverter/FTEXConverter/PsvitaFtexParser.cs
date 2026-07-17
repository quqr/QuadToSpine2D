using System.Text;
using VanillawareConverter.Common;
using VanillawareConverter.Ftex.Swizzling;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex.Parsers;

public class PsvitaFtexParser : BaseFtexParser
{
    private readonly S3tcTexture _s3tc = new();

    public override GamePlatform Platform => GamePlatform.PSVita;

    protected override int MinimumFileLength => 4;

    protected override bool CheckMagic(byte[] fileData)
    {
        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        return magic is "GXT\0" or "GXT";
    }

    protected override void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results)
    {
        var offset = 0x20;
        while (offset + 0x40 < fileData.Length)
        {
            var texOffset = (int)ByteHelper.ReadUInt32(fileData, offset + 0x00);
            var texSize = (int)ByteHelper.ReadUInt32(fileData, offset + 0x04);
            var palOffset = (int)ByteHelper.ReadUInt32(fileData, offset + 0x08);
            var palSize = (int)ByteHelper.ReadUInt32(fileData, offset + 0x0C);
            var w = (int)ByteHelper.ReadUInt16(fileData, offset + 0x10);
            var h = (int)ByteHelper.ReadUInt16(fileData, offset + 0x12);
            int fmt = fileData[offset + 0x14];
            int mipCount = fileData[offset + 0x15];
            int swizzle = fileData[offset + 0x16];

            if (texOffset == 0 || w == 0 || h == 0)
                break;

            if (texOffset + texSize > fileData.Length)
                break;

            var pixelData = new byte[texSize];
            Array.Copy(fileData, texOffset, pixelData, 0, texSize);

            var result = ProcessTexture(pixelData, w, h, fmt, swizzle);
            if (result != null) results.Add(result);

            offset += 0x40;
        }
    }

    private ImageResult? ProcessTexture(byte[] data, int w, int h, int fmt, int swizzle)
    {
        byte[] rgba;

        switch (fmt)
        {
            case 0x00:
                rgba = data;
                if (swizzle != 0) rgba = PsvitaSwizzle.BgraSwizzled(rgba, w, h);
                ConvertBgraToRgba(rgba);
                break;
            case 0x04:
            case 0x05:
                rgba = _s3tc.Dxt1(data);
                if (swizzle != 0) rgba = PsvitaSwizzle.DxtSwizzled(rgba, w, h);
                break;
            case 0x06:
            case 0x07:
                rgba = _s3tc.Dxt3(data);
                if (swizzle != 0) rgba = PsvitaSwizzle.DxtSwizzled(rgba, w, h);
                break;
            case 0x08:
            case 0x09:
                rgba = _s3tc.Dxt5(data);
                if (swizzle != 0) rgba = PsvitaSwizzle.DxtSwizzled(rgba, w, h);
                break;
            default:
                return null;
        }

        return new ImageResult(w, h, rgba);
    }

    private void ConvertBgraToRgba(byte[] data)
    {
        for (var i = 0; i < data.Length; i += 4)
        {
            var b = data[i + 0];
            data[i + 0] = data[i + 2];
            data[i + 2] = b;
        }
    }
}