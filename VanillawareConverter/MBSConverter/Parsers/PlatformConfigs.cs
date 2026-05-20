using System.Text;
using VanillawareConverter.Mbs;
using VanillawareConverter.Mbs.Models;

namespace VanillawareConverter.Mbs.Parsers;

public static class PlatformConfigs
{
    private static readonly Dictionary<PlatformTag, PlatformData> _configs = new()
    {
        [PlatformTag.Ps2Grim] = new PlatformData
        {
            IdTag = "ps2 grim grimoire",
            BigEndian = false,
            Sections = GetVer55Sections()
        },
        [PlatformTag.Ps2Odin] = new PlatformData
        {
            IdTag = "ps2 odin sphere",
            BigEndian = false,
            Sections = GetVer55Sections()
        },
        [PlatformTag.NdsKuma] = new PlatformData
        {
            IdTag = "nds kumatanchi",
            BigEndian = false,
            Sections = GetVer66Sections()
        },
        [PlatformTag.WiiMura] = new PlatformData
        {
            IdTag = "wii muramasa",
            BigEndian = true,
            Sections = GetVer66Sections()
        },
        [PlatformTag.Ps3Drag] = new PlatformData
        {
            IdTag = "ps3 dragon crown",
            BigEndian = true,
            Sections = GetVer6eSections()
        },
        [PlatformTag.Ps3Odin] = new PlatformData
        {
            IdTag = "ps3 odin sphere rehd",
            BigEndian = true,
            Sections = GetVer72Sections()
        },
        [PlatformTag.Ps4Odin] = new PlatformData
        {
            IdTag = "ps4 odin sphere rehd",
            BigEndian = false,
            Sections = GetVer72Sections()
        },
        [PlatformTag.Ps4Drag] = new PlatformData
        {
            IdTag = "ps4 dragon crown pro",
            BigEndian = false,
            Sections = GetVer72Sections()
        },
        [PlatformTag.Ps4Sent] = new PlatformData
        {
            IdTag = "ps4 13 sentinels",
            BigEndian = false,
            Sections = GetVer76Sections()
        },
        [PlatformTag.SwiSent] = new PlatformData
        {
            IdTag = "switch 13 sentinels",
            BigEndian = false,
            Sections = GetVer77Sections()
        },
        [PlatformTag.SwiGrim] = new PlatformData
        {
            IdTag = "switch grim grimoire hd",
            BigEndian = false,
            Sections = GetVer77Sections()
        },
        [PlatformTag.SwiUnic] = new PlatformData
        {
            IdTag = "switch unicorn overlord",
            BigEndian = false,
            Sections = GetVer77Sections()
        },
        [PlatformTag.Ps4Unic] = new PlatformData
        {
            IdTag = "ps4 unicorn overlord",
            BigEndian = false,
            Sections = GetVer77Sections()
        }
    };

    public static PlatformData? GetConfig(PlatformTag tag)
    {
        return _configs.TryGetValue(tag, out var config) ? config : null;
    }

    public static string GetTagString(PlatformTag tag)
    {
        return tag switch
        {
            PlatformTag.Ps2Grim => "ps2_grim",
            PlatformTag.Ps2Odin => "ps2_odin",
            PlatformTag.NdsKuma => "nds_kuma",
            PlatformTag.WiiMura => "wii_mura",
            PlatformTag.Ps3Drag => "ps3_drag",
            PlatformTag.Ps3Odin => "ps3_odin",
            PlatformTag.Ps4Odin => "ps4_odin",
            PlatformTag.Ps4Drag => "ps4_drag",
            PlatformTag.Ps4Sent => "ps4_sent",
            PlatformTag.SwiSent => "swi_sent",
            PlatformTag.SwiGrim => "swi_grim",
            PlatformTag.SwiUnic => "swi_unic",
            PlatformTag.Ps4Unic => "ps4_unic",
            _ => "unknown"
        };
    }

    public static PlatformTag DetectPlatform(byte[] fileData)
    {
        if (fileData == null || fileData.Length < 0x20)
            return PlatformTag.Unknown;

        var magic = Encoding.ASCII.GetString(fileData, 0, 4);

        if (magic == "FMBP")
        {
            var ver = ByteHelper.ReadUInt16(fileData, 0x14);
            return ver switch
            {
                0xc9 => PlatformTag.Ps2Grim,
                0x55 => PlatformTag.Ps2Odin,
                _ => PlatformTag.Unknown
            };
        }

        if (magic == "FMBS")
        {
            var verBig = ByteHelper.ReadUInt16(fileData, 0x14, true);
            switch (verBig)
            {
                case 0x66: return PlatformTag.WiiMura;
                case 0x6e: return PlatformTag.Ps3Drag;
                case 0x72: return PlatformTag.Ps3Odin;
            }

            var verLittle = ByteHelper.ReadUInt16(fileData, 0x14);
            switch (verLittle)
            {
                case 0x66: return PlatformTag.NdsKuma;
                case 0x76: return PlatformTag.Ps4Sent;
                case 0x77: return PlatformTag.SwiSent;
            }
        }

        return PlatformTag.Unknown;
    }

    private static List<SectionInfo> GetVer55Sections()
    {
        return
        [
            new(0x54, 0x20, [0x3c, 2]),
            new(0x58, 0x20, [0x3e, 2]),
            new(0x5c, 0x20, [0x40, 2]),
            new(0x60, 0x50, [0x44, 2]),
            new(0x64, 0x18, [0x42, 2]),
            new(0x68, 0x08, [0x46, 2]),
            new(0x6c, 0x18, [0x4c, 2]),
            new(0x70, 0x30, [0x48, 2]),
            new(0x74, 0x20, [0x4a, 2]),
            new(0x78, 0x30, [0x4e, 2]),
            new(0x7c, 0x08, [0x50, 2])
        ];
    }

    private static List<SectionInfo> GetVer66Sections()
    {
        return
        [
            new(0x54, 0x18, [0x3c, 2]),
            new(0x58, 0x30, [0x3e, 2]),
            new(0x5c, 0x30, [0x40, 2]),
            new(0x60, 0x50, [0x44, 2]),
            new(0x64, 0x0c, [0x42, 2]),
            new(0x68, 0x08, [0x46, 2]),
            new(0x6c, 0x18, [0x4c, 2]),
            new(0x70, 0x24, [0x48, 2]),
            new(0x74, 0x20, [0x4a, 2]),
            new(0x78, 0x30, [0x4e, 2]),
            new(0x7c, 0x10, [0x50, 2])
        ];
    }

    private static List<SectionInfo> GetVer6eSections()
    {
        return
        [
            new(0x74, 0x18, [0x54, 2]),
            new(0x78, 0x30, [0x56, 2]),
            new(0x7c, 0x30, [0x58, 2]),
            new(0x80, 0x50, [0x5a, 2]),
            new(0x84, 0x0c, [0x50, 4]),
            new(0x88, 0x08, [0x5c, 2]),
            new(0x8c, 0x1c, [0x64, 2]),
            new(0x90, 0x24, [0x5e, 2]),
            new(0x94, 0x20, [0x60, 2]),
            new(0x98, 0x30, [0x66, 2]),
            new(0x9c, 0x14, [0x68, 2])
        ];
    }

    private static List<SectionInfo> GetVer72Sections()
    {
        return
        [
            new(0xb0, 0x18, [0x54, 2]),
            new(0xb8, 0x30, [0x56, 2]),
            new(0xc0, 0x30, [0x58, 2]),
            new(0xc8, 0x50, [0x5a, 2]),
            new(0xd0, 0x10, [0x50, 4]),
            new(0xd8, 0x08, [0x5c, 2]),
            new(0xe0, 0x1c, [0x62, 2]),
            new(0xe8, 0x24, [0x5e, 2]),
            new(0xf0, 0x20, [0x60, 2]),
            new(0xf8, 0x30, [0x64, 2]),
            new(0x100, 0x18, [0x66, 2]),
            new(0x108, 0x14, [0x6a, 2])
        ];
    }

    private static List<SectionInfo> GetVer76Sections()
    {
        return
        [
            new(0xb0, 0x18, [0x54, 2]),
            new(0xb8, 0x30, [0x56, 2]),
            new(0xc0, 0x30, [0x58, 2]),
            new(0xc8, 0x50, [0x5a, 2]),
            new(0xd0, 0x14, [0x50, 4]),
            new(0xd8, 0x08, [0x5c, 2]),
            new(0xe0, 0x1c, [0x62, 2]),
            new(0xe8, 0x24, [0x5e, 2]),
            new(0xf0, 0x20, [0x60, 2]),
            new(0xf8, 0x30, [0x64, 2]),
            new(0x100, 0x18, [0x66, 2]),
            new(0x108, 0x14, [0x6a, 2])
        ];
    }

    private static List<SectionInfo> GetVer77Sections()
    {
        return
        [
            new(0xb0, 0x18, [0x54, 4]),
            new(0xb8, 0x30, [0x58, 4]),
            new(0xc0, 0x30, [0x5c, 4]),
            new(0xc8, 0x50, [0x60, 4]),
            new(0xd0, 0x14, [0x50, 4]),
            new(0xd8, 0x08, [0x64, 2]),
            new(0xe0, 0x1c, [0x6a, 2]),
            new(0xe8, 0x24, [0x66, 2]),
            new(0xf0, 0x20, [0x68, 2]),
            new(0xf8, 0x30, [0x6c, 2]),
            new(0x100, 0x18, [0x6e, 2]),
            new(0x108, 0x14, [0x72, 2])
        ];
    }
}