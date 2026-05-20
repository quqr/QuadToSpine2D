using VanillawareConverter.Mbs;
using VanillawareConverter.Mbs.Models;

namespace VanillawareConverter.Mbs.Parsers;

public class MbsToV55Parser
{
    private bool _bigEndian;
    private byte[] _fileData = [];
    private PlatformData _platform = new();

    public V55Data Parse(byte[] fileData, PlatformTag tag)
    {
        _fileData = fileData;
        var platform = PlatformConfigs.GetConfig(tag);
        if (platform == null)
            throw new NotSupportedException($"Unknown platform: {tag}");

        _platform = platform;
        _bigEndian = platform.BigEndian;

        var result = new V55Data
        {
            Tag = PlatformConfigs.GetTagString(tag),
            Id3 = platform.IdTag,
            Ver = "55"
        };

        var sections = _platform.Sections;
        if (sections.Count < 11)
            throw new NotSupportedException($"Invalid section count: {sections.Count}");

        result.S0 = ParseS0(sections[0]);
        result.S1 = ParseS1(sections[1]);
        result.S2 = ParseS2(sections[2]);
        result.S3 = ParseS3(sections[3]);
        result.S4 = ParseS4(sections[4]);
        result.S5 = ParseS5(sections[5]);
        result.S6 = ParseS6(sections[6]);
        result.S7 = ParseS7(sections[7]);
        result.S8 = ParseS8(sections[8]);
        result.S9 = ParseS9(sections[9]);
        result.Sa = ParseSa(sections[10]);
        result.Sb = sections.Count > 11 ? ParseSb(sections[11]) : [];

        return result;
    }

    private (int sp, int sc, int sk) GetSectionHead(SectionInfo sect)
    {
        var sp = ByteHelper.ReadInt(_fileData, sect.P, 4, _bigEndian);
        var sc = ByteHelper.ReadInt(_fileData, sect.C[0], sect.C[1], _bigEndian);
        var sk = sect.K;
        return (sp, sc, sk);
    }

    private List<S0Color?> ParseS0(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S0Color?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            object? fog = null;
            var tag = _platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                fog = ParsePs2Quad20c(s);
            else
                fog = ParseNdsQuad18c(s);

            result.Add(new S0Color
            {
                I = $"s0 {i}",
                Fog = fog
            });
        }

        return result;
    }

    private List<S1Source?> ParseS1(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S1Source?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            float[]? quad = null;
            var tag = _platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                quad = ParsePs2Quad20p(s, _bigEndian);
            else
                quad = ParseNdsQuad30p(s, _bigEndian);

            result.Add(new S1Source
            {
                I = $"s1 {i}",
                SrcQuad = quad
            });
        }

        return result;
    }

    private List<S2Dest?> ParseS2(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S2Dest?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            float[]? quad = null;
            var tag = _platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                quad = ParsePs2Quad20p(s, _bigEndian);
            else
                quad = ParseNdsQuad30p(s, _bigEndian);

            result.Add(new S2Dest
            {
                I = $"s2 {i}",
                DstQuad = quad
            });
        }

        return result;
    }

    private List<S3Hitbox?> ParseS3(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S3Hitbox?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var rect = new float[8];
            var xyz = new float[12];

            for (var j = 0; j < 8; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, _bigEndian);

            for (var j = 0; j < 12; j++)
                xyz[j] = ByteHelper.ReadFloat32(s, 0x20 + j * 4, _bigEndian);

            result.Add(new S3Hitbox
            {
                I = $"s3 {i}",
                Rect = rect,
                Xyz = xyz
            });
        }

        return result;
    }

    private List<S4Texture?> ParseS4(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S4Texture?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            int flags = 0, blendId = 0, texId = 0;
            var s0s1s2 = new int[3];
            int attrib = 0, colorId = 0;

            var tag = _platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
            {
                flags = ByteHelper.ReadInt(s, 0x00, 2, _bigEndian);
                blendId = ByteHelper.ReadInt(s, 0x02, 1, _bigEndian);
                texId = ByteHelper.ReadInt(s, 0x03, 1, _bigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x08, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x04, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x10, 2, _bigEndian)
                ];
            }
            else if (tag.Contains("nds_kuma") || tag.Contains("wii_mura"))
            {
                flags = ByteHelper.ReadInt(s, 0x00, 2, _bigEndian);
                blendId = ByteHelper.ReadInt(s, 0x02, 1, _bigEndian);
                texId = ByteHelper.ReadInt(s, 0x03, 1, _bigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x08, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x04, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x0a, 2, _bigEndian)
                ];
            }
            else
            {
                ByteHelper.ReadInt(s, 0x00, 4, _bigEndian);
                colorId = ByteHelper.ReadInt(s, 0x04, 1, _bigEndian);
                flags = ByteHelper.ReadInt(s, 0x05, 1, _bigEndian);
                blendId = ByteHelper.ReadInt(s, 0x06, 1, _bigEndian);
                texId = ByteHelper.ReadInt(s, 0x07, 1, _bigEndian);
                attrib = ByteHelper.ReadInt(s, 0x08, 4, _bigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x0e, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x0c, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x10, 2, _bigEndian)
                ];
            }

            result.Add(new S4Texture
            {
                I = $"s4 {i}",
                Blend = blendId,
                Tex = texId,
                S0S1S2 = s0s1s2,
                Bits = $"0x{flags:x}",
                Attr = $"0x{attrib:x}",
                Color = colorId
            });
        }

        return result;
    }

    private List<S5HitboxRef?> ParseS5(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S5HitboxRef?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var s3Id = ByteHelper.ReadInt(s, 0x00, 2, _bigEndian);
            var flags = ByteHelper.ReadInt(s, 0x04, 4, _bigEndian);

            result.Add(new S5HitboxRef
            {
                I = $"s5 {i}",
                S3 = s3Id,
                Bits = $"0x{flags:x}"
            });
        }

        return result;
    }

    private List<S6Keyframe?> ParseS6(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S6Keyframe?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var rect = new float[4];
            for (var j = 0; j < 4; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, _bigEndian);

            var s4 = new int[2];
            var s5 = new int[2];
            var flags = 0;

            var tag = _platform.IdTag;
            if (tag.Contains("ps2") || tag.Contains("nds_kuma") || tag.Contains("wii_mura"))
            {
                s4 =
                [
                    ByteHelper.ReadInt(s, 0x10, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x14, 1, _bigEndian)
                ];
                s5 =
                [
                    ByteHelper.ReadInt(s, 0x12, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x15, 1, _bigEndian)
                ];
                flags = ByteHelper.ReadInt(s, 0x16, 2, _bigEndian);
            }
            else
            {
                s4 =
                [
                    ByteHelper.ReadInt(s, 0x10, 4, _bigEndian),
                    ByteHelper.ReadInt(s, 0x16, 2, _bigEndian)
                ];
                s5 =
                [
                    ByteHelper.ReadInt(s, 0x14, 2, _bigEndian),
                    ByteHelper.ReadInt(s, 0x18, 1, _bigEndian)
                ];
                flags = ByteHelper.ReadInt(s, 0x19, 1, _bigEndian);
            }

            result.Add(new S6Keyframe
            {
                I = $"s6 {i}",
                Rect = rect,
                S4 = s4,
                S5 = s5,
                Bits = $"0x{flags:x}"
            });
        }

        return result;
    }

    private List<S7Transform?> ParseS7(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S7Transform?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var move = new float[3];
            var rotate = new float[3];
            var scale = new float[2];
            var fog = "#ffffffff";

            var tag = _platform.IdTag;
            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
            {
                var r = ByteHelper.ReadFloat32(s, 0x00);
                var g = ByteHelper.ReadFloat32(s, 0x04);
                var b = ByteHelper.ReadFloat32(s, 0x08);
                var a = ByteHelper.ReadFloat32(s, 0x0c);
                fog =
                    $"#{(int)(r * 255) & 0xFF:x2}{(int)(g * 255) & 0xFF:x2}{(int)(b * 255) & 0xFF:x2}{(int)(a * 255) & 0xFF:x2}";
                move =
                [
                    ByteHelper.ReadFloat32(s, 0x10),
                    ByteHelper.ReadFloat32(s, 0x14),
                    ByteHelper.ReadFloat32(s, 0x18)
                ];
                rotate =
                [
                    ByteHelper.ReadFloat32(s, 0x1c),
                    ByteHelper.ReadFloat32(s, 0x20),
                    ByteHelper.ReadFloat32(s, 0x24)
                ];
                scale =
                [
                    ByteHelper.ReadFloat32(s, 0x28),
                    ByteHelper.ReadFloat32(s, 0x2c)
                ];
            }
            else
            {
                move =
                [
                    ByteHelper.ReadFloat32(s, 0x00, _bigEndian),
                    ByteHelper.ReadFloat32(s, 0x04, _bigEndian),
                    ByteHelper.ReadFloat32(s, 0x08, _bigEndian)
                ];
                rotate =
                [
                    ByteHelper.ReadFloat32(s, 0x0c, _bigEndian),
                    ByteHelper.ReadFloat32(s, 0x10, _bigEndian),
                    ByteHelper.ReadFloat32(s, 0x14, _bigEndian)
                ];
                scale =
                [
                    ByteHelper.ReadFloat32(s, 0x18, _bigEndian),
                    ByteHelper.ReadFloat32(s, 0x1c, _bigEndian)
                ];
                fog = ByteHelper.ReadHexStringWithPrefix(s, 0x20, 4);
            }

            result.Add(new S7Transform
            {
                I = $"s7 {i}",
                Move = move,
                Rotate = rotate,
                Scale = scale,
                Fog = fog
            });
        }

        return result;
    }

    private List<S8AnimFrame?> ParseS8(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S8AnimFrame?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var s6Id = ByteHelper.ReadInt(s, 0x00, 2, _bigEndian);
            var s7Id = ByteHelper.ReadInt(s, 0x04, 2, _bigEndian);
            var time = ByteHelper.ReadInt(s, 0x06, 2, _bigEndian);
            var flags = ByteHelper.ReadInt(s, 0x08, 4, _bigEndian);
            var loop = ByteHelper.ReadInt(s, 0x0c, 2, _bigEndian);
            var inS5S3 = ByteHelper.ReadInt(s, 0x0e, 1, _bigEndian);
            var inS7 = ByteHelper.ReadInt(s, 0x10, 1, _bigEndian);
            var inS6 = ByteHelper.ReadInt(s, 0x11, 1, _bigEndian);
            var inS0S1S2 = ByteHelper.ReadInt(s, 0x12, 1, _bigEndian);
            var sfx = ByteHelper.ReadInt(s, 0x1c, 4, _bigEndian);

            result.Add(new S8AnimFrame
            {
                I = $"s8 {i}",
                S6 = s6Id,
                S7 = s7Id,
                Time = time,
                Loop = loop,
                Sfx = sfx,
                Bits = $"0x{flags:x}",
                InS5S3 = inS5S3,
                InS7 = inS7,
                InS6 = inS6,
                InS0S1S2 = inS0S1S2
            });
        }

        return result;
    }

    private List<S9Bone?> ParseS9(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<S9Bone?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var rect = new float[4];
            for (var j = 0; j < 4; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, _bigEndian);

            var name = ByteHelper.ReadNullTerminatedString(s, 0x10);
            var saSetId = ByteHelper.ReadInt(s, 0x28, 2, _bigEndian);
            var saSetNo = ByteHelper.ReadInt(s, 0x2a, 1, _bigEndian);

            result.Add(new S9Bone
            {
                I = $"s9 {i}",
                Rect = rect,
                Name = name,
                Sa = [saSetId, saSetNo]
            });
        }

        return result;
    }

    private List<SaAnimation?> ParseSa(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<SaAnimation?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(_fileData, p, sk);

            var s8SetId = ByteHelper.ReadInt(s, 0x00, 2, _bigEndian);
            var s8SetNo = ByteHelper.ReadInt(s, 0x02, 2, _bigEndian);
            var s8SetSum = ByteHelper.ReadInt(s, 0x04, 4, _bigEndian);
            var s8SetSt = ByteHelper.ReadInt(s, 0x13, 1, _bigEndian);

            result.Add(new SaAnimation
            {
                I = $"sa {i}",
                S8 = [s8SetId + s8SetSt, s8SetNo - s8SetSt, s8SetSum]
            });
        }

        return result;
    }

    private List<SbExtension?> ParseSb(SectionInfo sect)
    {
        var (sp, sc, sk) = GetSectionHead(sect);
        var result = new List<SbExtension?>();

        for (var i = 0; i < sc; i++) result.Add(new SbExtension { I = $"sb {i}" });

        return result;
    }

    private static object ParsePs2Quad20c(byte[] s)
    {
        var adjusted = new byte[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            int b = s[i];
            b = (b << 1) & 0xFF;
            if (b > 255) b = 255;
            adjusted[i] = (byte)b;
        }

        return ByteHelper.ReadHexStringWithPrefix(adjusted, 0x08, 4);
    }

    private static object ParseNdsQuad18c(byte[] s)
    {
        return ByteHelper.ReadHexStringWithPrefix(s, 0x04, 4);
    }

    private static float[] ParsePs2Quad20p(byte[] s, bool bigEndian)
    {
        var floats = new float[16];
        for (var i = 0; i < 16; i++)
        {
            var val = ByteHelper.ReadInt16(s, i * 2, bigEndian);
            floats[i] = val / 16.0f;
        }

        return
        [
            floats[4], floats[5],
            floats[6], floats[7],
            floats[8], floats[9],
            floats[10], floats[11]
        ];
    }

    private static float[] ParseNdsQuad30p(byte[] s, bool bigEndian)
    {
        var floats = new float[12];
        for (var i = 0; i < 12; i++) floats[i] = ByteHelper.ReadFloat32(s, i * 4, bigEndian);

        return
        [
            floats[2], floats[3],
            floats[4], floats[5],
            floats[6], floats[7],
            floats[8], floats[9]
        ];
    }
}