using VanillawareConverter.Common;
using VanillawareConverter.Mbs.Models;

namespace VanillawareConverter.Mbs.Parsers;

public class MbsToV55Parser
{
    public V55Data Parse(byte[] fileData, PlatformTag tag)
    {
        var platform = PlatformConfigs.GetConfig(tag)
                       ?? throw new NotSupportedException($"Unknown platform: {tag}");

        var ctx = new ParseContext(fileData, platform.BigEndian, platform);

        var result = new V55Data
        {
            Tag = PlatformConfigs.GetTagString(tag),
            Id3 = platform.IdTag,
            Ver = "55"
        };

        var sections = platform.Sections;
        if (sections.Count < 11)
            throw new NotSupportedException($"Invalid section count: {sections.Count}");

        result.S0 = ParseS0(ctx, sections[0]);
        result.S1 = ParseS1(ctx, sections[1]);
        result.S2 = ParseS2(ctx, sections[2]);
        result.S3 = ParseS3(ctx, sections[3]);
        result.S4 = ParseS4(ctx, sections[4]);
        result.S5 = ParseS5(ctx, sections[5]);
        result.S6 = ParseS6(ctx, sections[6]);
        result.S7 = ParseS7(ctx, sections[7]);
        result.S8 = ParseS8(ctx, sections[8]);
        result.S9 = ParseS9(ctx, sections[9]);
        result.Sa = ParseSa(ctx, sections[10]);
        result.Sb = sections.Count > 11 ? ParseSb(ctx, sections[11]) : [];

        return result;
    }

    private static (int sp, int sc, int sk) GetSectionHead(ParseContext ctx, SectionInfo sect)
    {
        var sp = ByteHelper.ReadInt(ctx.FileData, sect.P, 4, ctx.BigEndian);
        var sc = ByteHelper.ReadInt(ctx.FileData, sect.C[0], sect.C[1], ctx.BigEndian);
        var sk = sect.K;
        return (sp, sc, sk);
    }

    private static List<T?> ParseSection<T>(ParseContext ctx, SectionInfo sect, Func<byte[], int, T> parseEntry)
        where T : class
    {
        var (sp, sc, sk) = GetSectionHead(ctx, sect);
        var result = new List<T?>();

        for (var i = 0; i < sc; i++)
        {
            var p = sp + i * sk;
            var s = ByteHelper.SubArray(ctx.FileData, p, sk);
            result.Add(parseEntry(s, i));
        }

        return result;
    }

    private static List<S0Color?> ParseS0(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            object? fog = null;
            var tag = ctx.Platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                fog = ParsePs2Quad20c(s);
            else
                fog = ParseNdsQuad18c(s);

            return new S0Color
            {
                I = $"s0 {i}",
                Fog = fog
            };
        });
    }

    private static List<S1Source?> ParseS1(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            float[]? quad = null;
            var tag = ctx.Platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                quad = ParsePs2Quad20p(s, ctx.BigEndian);
            else
                quad = ParseNdsQuad30p(s, ctx.BigEndian);

            return new S1Source
            {
                I = $"s1 {i}",
                SrcQuad = quad
            };
        });
    }

    private static List<S2Dest?> ParseS2(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            float[]? quad = null;
            var tag = ctx.Platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
                quad = ParsePs2Quad20p(s, ctx.BigEndian);
            else
                quad = ParseNdsQuad30p(s, ctx.BigEndian);

            return new S2Dest
            {
                I = $"s2 {i}",
                DstQuad = quad
            };
        });
    }

    private static List<S3Hitbox?> ParseS3(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var rect = new float[8];
            var xyz = new float[12];

            for (var j = 0; j < 8; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, ctx.BigEndian);

            for (var j = 0; j < 12; j++)
                xyz[j] = ByteHelper.ReadFloat32(s, 0x20 + j * 4, ctx.BigEndian);

            return new S3Hitbox
            {
                I = $"s3 {i}",
                Rect = rect,
                Xyz = xyz
            };
        });
    }

    private static List<S4Texture?> ParseS4(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            int flags = 0, blendId = 0, texId = 0;
            var s0s1s2 = new int[3];
            int attrib = 0, colorId = 0;

            var tag = ctx.Platform.IdTag;

            if (tag.Contains("ps2") && (tag.Contains("grim") || tag.Contains("odin")))
            {
                flags = ByteHelper.ReadInt(s, 0x00, 2, ctx.BigEndian);
                blendId = ByteHelper.ReadInt(s, 0x02, 1, ctx.BigEndian);
                texId = ByteHelper.ReadInt(s, 0x03, 1, ctx.BigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x08, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x04, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x10, 2, ctx.BigEndian)
                ];
            }
            else if (tag.Contains("nds_kuma") || tag.Contains("wii_mura"))
            {
                flags = ByteHelper.ReadInt(s, 0x00, 2, ctx.BigEndian);
                blendId = ByteHelper.ReadInt(s, 0x02, 1, ctx.BigEndian);
                texId = ByteHelper.ReadInt(s, 0x03, 1, ctx.BigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x08, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x04, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x0a, 2, ctx.BigEndian)
                ];
            }
            else
            {
                ByteHelper.ReadInt(s, 0x00, 4, ctx.BigEndian);
                colorId = ByteHelper.ReadInt(s, 0x04, 1, ctx.BigEndian);
                flags = ByteHelper.ReadInt(s, 0x05, 1, ctx.BigEndian);
                blendId = ByteHelper.ReadInt(s, 0x06, 1, ctx.BigEndian);
                texId = ByteHelper.ReadInt(s, 0x07, 1, ctx.BigEndian);
                attrib = ByteHelper.ReadInt(s, 0x08, 4, ctx.BigEndian);
                s0s1s2 =
                [
                    ByteHelper.ReadInt(s, 0x0e, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x0c, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x10, 2, ctx.BigEndian)
                ];
            }

            return new S4Texture
            {
                I = $"s4 {i}",
                Blend = blendId,
                Tex = texId,
                S0S1S2 = s0s1s2,
                Bits = $"0x{flags:x}",
                Attr = $"0x{attrib:x}",
                Color = colorId
            };
        });
    }

    private static List<S5HitboxRef?> ParseS5(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var s3Id = ByteHelper.ReadInt(s, 0x00, 2, ctx.BigEndian);
            var flags = ByteHelper.ReadInt(s, 0x04, 4, ctx.BigEndian);

            return new S5HitboxRef
            {
                I = $"s5 {i}",
                S3 = s3Id,
                Bits = $"0x{flags:x}"
            };
        });
    }

    private static List<S6Keyframe?> ParseS6(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var rect = new float[4];
            for (var j = 0; j < 4; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, ctx.BigEndian);

            var s4 = new int[2];
            var s5 = new int[2];
            var flags = 0;

            var tag = ctx.Platform.IdTag;
            if (tag.Contains("ps2") || tag.Contains("nds_kuma") || tag.Contains("wii_mura"))
            {
                s4 =
                [
                    ByteHelper.ReadInt(s, 0x10, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x14, 1, ctx.BigEndian)
                ];
                s5 =
                [
                    ByteHelper.ReadInt(s, 0x12, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x15, 1, ctx.BigEndian)
                ];
                flags = ByteHelper.ReadInt(s, 0x16, 2, ctx.BigEndian);
            }
            else
            {
                s4 =
                [
                    ByteHelper.ReadInt(s, 0x10, 4, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x16, 2, ctx.BigEndian)
                ];
                s5 =
                [
                    ByteHelper.ReadInt(s, 0x14, 2, ctx.BigEndian),
                    ByteHelper.ReadInt(s, 0x18, 1, ctx.BigEndian)
                ];
                flags = ByteHelper.ReadInt(s, 0x19, 1, ctx.BigEndian);
            }

            return new S6Keyframe
            {
                I = $"s6 {i}",
                Rect = rect,
                S4 = s4,
                S5 = s5,
                Bits = $"0x{flags:x}"
            };
        });
    }

    private static List<S7Transform?> ParseS7(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var move = new float[3];
            var rotate = new float[3];
            var scale = new float[2];
            var fog = "#ffffffff";

            var tag = ctx.Platform.IdTag;
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
                    ByteHelper.ReadFloat32(s, 0x00, ctx.BigEndian),
                    ByteHelper.ReadFloat32(s, 0x04, ctx.BigEndian),
                    ByteHelper.ReadFloat32(s, 0x08, ctx.BigEndian)
                ];
                rotate =
                [
                    ByteHelper.ReadFloat32(s, 0x0c, ctx.BigEndian),
                    ByteHelper.ReadFloat32(s, 0x10, ctx.BigEndian),
                    ByteHelper.ReadFloat32(s, 0x14, ctx.BigEndian)
                ];
                scale =
                [
                    ByteHelper.ReadFloat32(s, 0x18, ctx.BigEndian),
                    ByteHelper.ReadFloat32(s, 0x1c, ctx.BigEndian)
                ];
                fog = ByteHelper.ReadHexStringWithPrefix(s, 0x20, 4);
            }

            return new S7Transform
            {
                I = $"s7 {i}",
                Move = move,
                Rotate = rotate,
                Scale = scale,
                Fog = fog
            };
        });
    }

    private static List<S8AnimFrame?> ParseS8(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var s6Id = ByteHelper.ReadInt(s, 0x00, 2, ctx.BigEndian);
            var s7Id = ByteHelper.ReadInt(s, 0x04, 2, ctx.BigEndian);
            var time = ByteHelper.ReadInt(s, 0x06, 2, ctx.BigEndian);
            var flags = ByteHelper.ReadInt(s, 0x08, 4, ctx.BigEndian);
            var loop = ByteHelper.ReadInt(s, 0x0c, 2, ctx.BigEndian);
            var inS5S3 = ByteHelper.ReadInt(s, 0x0e, 1, ctx.BigEndian);
            var inS7 = ByteHelper.ReadInt(s, 0x10, 1, ctx.BigEndian);
            var inS6 = ByteHelper.ReadInt(s, 0x11, 1, ctx.BigEndian);
            var inS0S1S2 = ByteHelper.ReadInt(s, 0x12, 1, ctx.BigEndian);
            var sfx = ByteHelper.ReadInt(s, 0x1c, 4, ctx.BigEndian);

            return new S8AnimFrame
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
            };
        });
    }

    private static List<S9Bone?> ParseS9(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var rect = new float[4];
            for (var j = 0; j < 4; j++)
                rect[j] = ByteHelper.ReadFloat32(s, j * 4, ctx.BigEndian);

            var name = ByteHelper.ReadNullTerminatedString(s, 0x10);
            var saSetId = ByteHelper.ReadInt(s, 0x28, 2, ctx.BigEndian);
            var saSetNo = ByteHelper.ReadInt(s, 0x2a, 1, ctx.BigEndian);

            return new S9Bone
            {
                I = $"s9 {i}",
                Rect = rect,
                Name = name,
                Sa = [saSetId, saSetNo]
            };
        });
    }

    private static List<SaAnimation?> ParseSa(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (s, i) =>
        {
            var s8SetId = ByteHelper.ReadInt(s, 0x00, 2, ctx.BigEndian);
            var s8SetNo = ByteHelper.ReadInt(s, 0x02, 2, ctx.BigEndian);
            var s8SetSum = ByteHelper.ReadInt(s, 0x04, 4, ctx.BigEndian);
            var s8SetSt = ByteHelper.ReadInt(s, 0x13, 1, ctx.BigEndian);

            return new SaAnimation
            {
                I = $"sa {i}",
                S8 = [s8SetId + s8SetSt, s8SetNo - s8SetSt, s8SetSum]
            };
        });
    }

    private static List<SbExtension?> ParseSb(ParseContext ctx, SectionInfo sect)
    {
        return ParseSection(ctx, sect, (_, i) => new SbExtension { I = $"sb {i}" });
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

    private readonly struct ParseContext(byte[] fileData, bool bigEndian, PlatformData platform)
    {
        public readonly byte[] FileData = fileData;
        public readonly bool BigEndian = bigEndian;
        public readonly PlatformData Platform = platform;
    }
}