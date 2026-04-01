using System.Text;
using VanillawareConverter.Ftex.Parsers;

namespace VanillawareConverter.Ftex;

public static class PlatformDetector
{
    public static GamePlatform DetectPlatform(byte[] fileData)
    {
        if (fileData == null || fileData.Length < 4)
            return GamePlatform.Unknown;

        var magic = Encoding.ASCII.GetString(fileData, 0, Math.Min(4, fileData.Length));

        switch (magic)
        {
            case "FTEX":
                return DetectFtexPlatform(fileData);
            case "MIG.":
                return GamePlatform.PSP;
            case "GXT":
                return GamePlatform.PSVita;
            case "BIT":
                return GamePlatform.NDS;
            case "FCMP":
                return GamePlatform.Wii;
            case "gtf":
                return GamePlatform.PS3;
            default:
                if (fileData.Length >= 4)
                {
                    var magicNum = BitConverter.ToUInt32(fileData, 0);
                    if (magicNum == 0x20af30)
                        return GamePlatform.Wii;
                }

                break;
        }

        return GamePlatform.Unknown;
    }

    private static GamePlatform DetectFtexPlatform(byte[] fileData)
    {
        if (fileData.Length < 0x10)
            return GamePlatform.Unknown;

        if (fileData.Length < 0x20)
            return GamePlatform.Switch;

        var st = 0x20;
        if (st + 4 > fileData.Length)
            return GamePlatform.Switch;

        var ftxMagic = Encoding.ASCII.GetString(fileData, st, 4);
        if (ftxMagic != "FTX0")
            return GamePlatform.Switch;

        if (st + 0x10 > fileData.Length)
            return GamePlatform.Switch;

        var sz2 = (int)ByteHelper.ReadUInt32(fileData, st + 8);
        if (st + sz2 + 4 > fileData.Length)
            return GamePlatform.Switch;

        var texMagic = Encoding.ASCII.GetString(fileData, st + sz2, 4);
        switch (texMagic)
        {
            case ".tex":
                break;
            case "FGST":
                return GamePlatform.PS2;
        }

        return GamePlatform.Switch;
    }

    public static IFtexParser? CreateParser(GamePlatform platform)
    {
        return platform switch
        {
            GamePlatform.PS2 => new Ps2FtexParser(),
            GamePlatform.PS3 => new Ps3FtexParser(),
            GamePlatform.PS4 => new Ps4FtexParser(),
            GamePlatform.PSP => new PspFtexParser(),
            GamePlatform.PSVita => new PsvitaFtexParser(),
            GamePlatform.NDS => new NdsFtexParser(),
            GamePlatform.Wii => new WiiFtexParser(),
            GamePlatform.Switch => new SwitchFtexParser(),
            _ => null
        };
    }
}