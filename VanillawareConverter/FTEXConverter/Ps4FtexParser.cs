using System.Text;
using VanillawareConverter.Common;
using VanillawareConverter.Ftex.Swizzling;
using VanillawareConverter.Ftex.Textures;

namespace VanillawareConverter.Ftex.Parsers;

public class Ps4FtexParser : BaseFtexParser
{
    private readonly BptcTexture _bptc = new();
    private readonly S3tcTexture _s3tc = new();

    public override GamePlatform Platform => GamePlatform.PS4;

    protected override int MinimumFileLength => 4;

    protected override bool CheckMagic(byte[] fileData)
    {
        var magic = Encoding.ASCII.GetString(fileData, 0, 4);
        return magic == "FTEX";
    }

    protected override void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results)
    {
        var prefix = outputPrefix;
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
            if (ftxMagic != "FTX0") break;

            var sz1 = (int)ByteHelper.ReadUInt32(fileData, st + 4);
            var sz2 = (int)ByteHelper.ReadUInt32(fileData, st + 8);

            var result = ParseNvt(fileData, st + sz2);
            if (result != null) results.Add(result);

            st += sz1 + sz2;
        }
    }

    private ImageResult? ParseNvt(byte[] file, int baseOffset)
    {
        if (baseOffset + 4 > file.Length)
            return null;

        var magic = Encoding.ASCII.GetString(file, baseOffset, 4);
        if (magic != ".tex")
            return null;

        if (baseOffset + 36 > file.Length)
            return null;

        var fmt = (TextureFormat)ByteHelper.ReadUInt16(file, baseOffset + 4);
        var w = (int)ByteHelper.ReadUInt32(file, baseOffset + 12);
        var h = (int)ByteHelper.ReadUInt32(file, baseOffset + 16);
        var sz1 = (int)ByteHelper.ReadUInt32(file, baseOffset + 28);
        var sz2 = (int)ByteHelper.ReadUInt32(file, baseOffset + 32);

        ImageResult? img = null;
        switch (fmt)
        {
            case TextureFormat.BC3:
                img = ImBc3(file, baseOffset + sz1, w, h, sz2);
                break;
            case TextureFormat.BC4:
                img = ImBc4(file, baseOffset + sz1, w, h, sz2);
                break;
            case TextureFormat.BC7:
                img = ImBc7(file, baseOffset + sz1, w, h, sz2);
                break;
        }

        return img;
    }

    private ImageResult ImBc3(byte[] file, int pos, int w, int h, int size)
    {
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _s3tc.Bc3(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle16Bits(pix, w, ch);

        return new ImageResult(w, h, pix);
    }

    private ImageResult ImBc4(byte[] file, int pos, int w, int h, int size)
    {
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _s3tc.Bc4(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle8Bits(pix, w, ch);

        var result = new ImageResult(w, h, pix)
        {
            ColorCount = 0x100,
            Palette = ByteHelper.GrayClut(0x100)
        };
        return result;
    }

    private ImageResult ImBc7(byte[] file, int pos, int w, int h, int size)
    {
        var pix = new byte[size];
        Array.Copy(file, pos, pix, 0, size);

        pix = _bptc.Bc7(pix);

        var ch = ByteHelper.IntCeilPow2(h);
        pix = TegraX1Swizzle.Swizzle16Bits(pix, w, ch);

        return new ImageResult(w, h, pix);
    }
}