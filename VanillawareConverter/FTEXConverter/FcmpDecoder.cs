using System.Text;
using VanillawareConverter.Common;

namespace VanillawareConverter.Ftex;

public static class FcmpDecoder
{
    public static byte[] Decode(byte[]? data)
    {
        if (data == null || data.Length < 0x10)
            return [];

        var magic = Encoding.ASCII.GetString(data, 0, 4);
        if (magic != "FCMP")
            return [];

        var decSize = (int)ByteHelper.ReadUInt32(data, 4);
        var cmpSize = (int)ByteHelper.ReadUInt32(data, 8);

        if (cmpSize == 0)
            cmpSize = data.Length - 0x10;

        var result = new byte[decSize];
        var dict = new byte[0x1000];
        var dictPos = 0xFEE;

        var srcPos = 0x10;
        var dstPos = 0;
        var bitPos = 0;
        byte flags = 0;

        while (srcPos < 0x10 + cmpSize && dstPos < decSize)
        {
            if (bitPos == 0)
            {
                flags = data[srcPos++];
                bitPos = 8;
            }

            if ((flags & 1) != 0)
            {
                if (srcPos >= data.Length || dstPos >= decSize)
                    break;

                var b = data[srcPos++];
                result[dstPos++] = b;
                dict[dictPos] = b;
                dictPos = (dictPos + 1) & 0xFFF;
            }
            else
            {
                if (srcPos + 1 >= data.Length)
                    break;

                var b1 = data[srcPos++];
                var b2 = data[srcPos++];

                var offset = ((b2 & 0xF0) << 4) | b1;
                var length = (b2 & 0x0F) + 3;

                for (var i = 0; i < length && dstPos < decSize; i++)
                {
                    var b = dict[offset];
                    result[dstPos++] = b;
                    dict[dictPos] = b;
                    dictPos = (dictPos + 1) & 0xFFF;
                    offset = (offset + 1) & 0xFFF;
                }
            }

            flags >>= 1;
            bitPos--;
        }

        return result;
    }

    public static bool IsFcmpFile(byte[]? data)
    {
        if (data == null || data.Length < 4)
            return false;

        var magic = Encoding.ASCII.GetString(data, 0, 4);
        return magic == "FCMP";
    }
}