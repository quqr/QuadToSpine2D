using VanillawareConverter.Common;

namespace Tests;

public class ByteHelperTests
{
    #region 数组创建

    [Fact]
    public void ZeroBytes_CreatesZeroFilledArray()
    {
        var result = ByteHelper.ZeroBytes(5);
        Assert.Equal(5, result.Length);
        Assert.All(result, b => Assert.Equal(0, b));
    }

    [Fact]
    public void ByteRepeat_CreatesFilledArray()
    {
        var result = ByteHelper.ByteRepeat(0xAB, 4);
        Assert.Equal([0xAB, 0xAB, 0xAB, 0xAB], result);
    }

    #endregion

    #region ReadFloat32

    [Fact]
    public void ReadFloat32_LittleEndian_ReturnsCorrectValue()
    {
        // 1.0f in little-endian: 0x00 0x00 0x80 0x3F
        var data = new byte[] { 0x00, 0x00, 0x80, 0x3F };
        var result = ByteHelper.ReadFloat32(data, 0);
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void ReadFloat32_BigEndian_ReturnsCorrectValue()
    {
        // 1.0f in big-endian: 0x3F 0x80 0x00 0x00
        var data = new byte[] { 0x3F, 0x80, 0x00, 0x00 };
        var result = ByteHelper.ReadFloat32(data, 0, true);
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void ReadFloat32_NegativeValue_LittleEndian()
    {
        // -1.0f in little-endian: 0x00 0x00 0x80 0xBF
        var data = new byte[] { 0x00, 0x00, 0x80, 0xBF };
        var result = ByteHelper.ReadFloat32(data, 0);
        Assert.Equal(-1.0f, result);
    }

    [Fact]
    public void ReadFloat32_WithOffset_LittleEndian()
    {
        var data = new byte[] { 0xFF, 0x00, 0x00, 0x80, 0x3F };
        var result = ByteHelper.ReadFloat32(data, 1);
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void ReadFloat32_ThrowsWhenOffsetOutOfRange()
    {
        var data = new byte[] { 0x00, 0x01 };
        Assert.Throws<ArgumentOutOfRangeException>(() => ByteHelper.ReadFloat32(data, 0));
    }

    #endregion

    #region ReadInt16 / ReadUInt16

    [Fact]
    public void ReadInt16_LittleEndian_Positive()
    {
        var data = new byte[] { 0x01, 0x02 };
        Assert.Equal(0x0201, ByteHelper.ReadInt16(data, 0));
    }

    [Fact]
    public void ReadInt16_BigEndian_Positive()
    {
        var data = new byte[] { 0x01, 0x02 };
        Assert.Equal(0x0102, ByteHelper.ReadInt16(data, 0, true));
    }

    [Fact]
    public void ReadInt16_LittleEndian_Negative()
    {
        // -1 in little-endian: 0xFF 0xFF
        var data = new byte[] { 0xFF, 0xFF };
        Assert.Equal(-1, ByteHelper.ReadInt16(data, 0));
    }

    [Fact]
    public void ReadUInt16_LittleEndian()
    {
        var data = new byte[] { 0xFF, 0xFF };
        Assert.Equal((ushort)0xFFFF, ByteHelper.ReadUInt16(data, 0));
    }

    [Fact]
    public void ReadUInt16_BigEndian()
    {
        var data = new byte[] { 0x01, 0x00 };
        Assert.Equal((ushort)0x0100, ByteHelper.ReadUInt16(data, 0, true));
    }

    [Fact]
    public void ReadInt16_ThrowsWhenOffsetOutOfRange()
    {
        var data = new byte[] { 0x01 };
        Assert.Throws<ArgumentOutOfRangeException>(() => ByteHelper.ReadInt16(data, 0));
    }

    #endregion

    #region ReadInt32 / ReadUInt32

    [Fact]
    public void ReadInt32_LittleEndian()
    {
        // 0x12345678 in little-endian
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        Assert.Equal(0x12345678, ByteHelper.ReadInt32(data, 0));
    }

    [Fact]
    public void ReadInt32_BigEndian()
    {
        // 0x12345678 in big-endian
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        Assert.Equal(0x12345678, ByteHelper.ReadInt32(data, 0, true));
    }

    [Fact]
    public void ReadInt32_LittleEndian_Negative()
    {
        // -1 in little-endian
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        Assert.Equal(-1, ByteHelper.ReadInt32(data, 0));
    }

    [Fact]
    public void ReadUInt32_LittleEndian()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        Assert.Equal(0xFFFFFFFF, ByteHelper.ReadUInt32(data, 0));
    }

    [Fact]
    public void ReadInt32_WithOffset()
    {
        var data = new byte[] { 0x00, 0x78, 0x56, 0x34, 0x12 };
        Assert.Equal(0x12345678, ByteHelper.ReadInt32(data, 1));
    }

    [Fact]
    public void ReadInt32_ThrowsWhenOffsetOutOfRange()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        Assert.Throws<ArgumentOutOfRangeException>(() => ByteHelper.ReadInt32(data, 0));
    }

    #endregion

    #region ReadInt (generic)

    [Theory]
    [InlineData(1, false, 0x7F)]
    [InlineData(1, true, 0x7F)] // sbyte
    public void ReadInt_1Byte_ReturnsCorrectValue(int byteCount, bool signed, int expected)
    {
        var data = new byte[] { 0x7F };
        Assert.Equal(expected, ByteHelper.ReadInt(data, 0, byteCount, signed: signed));
    }

    [Fact]
    public void ReadInt_SignedByte_Negative()
    {
        var data = new byte[] { 0xFF }; // -1 as sbyte
        Assert.Equal(-1, ByteHelper.ReadInt(data, 0, 1, signed: true));
    }

    [Fact]
    public void ReadInt_2Bytes_LittleEndian()
    {
        var data = new byte[] { 0x01, 0x02 };
        Assert.Equal(0x0201, ByteHelper.ReadInt(data, 0, 2));
    }

    [Fact]
    public void ReadInt_4Bytes_LittleEndian()
    {
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        Assert.Equal(0x12345678, ByteHelper.ReadInt(data, 0, 4));
    }

    [Fact]
    public void ReadInt_InvalidByteCount_Throws()
    {
        var data = new byte[] { 0x01 };
        Assert.Throws<ArgumentException>(() => ByteHelper.ReadInt(data, 0, 3));
    }

    #endregion

    #region WriteUInt16 / WriteUInt32

    [Fact]
    public void WriteUInt16_LittleEndian()
    {
        var data = new byte[4];
        ByteHelper.WriteUInt16(data, 0, 0x1234);
        Assert.Equal(0x34, data[0]);
        Assert.Equal(0x12, data[1]);
    }

    [Fact]
    public void WriteUInt32_LittleEndian()
    {
        var data = new byte[8];
        ByteHelper.WriteUInt32(data, 0, 0x12345678);
        Assert.Equal(0x78, data[0]);
        Assert.Equal(0x56, data[1]);
        Assert.Equal(0x34, data[2]);
        Assert.Equal(0x12, data[3]);
    }

    [Fact]
    public void WriteUInt32_WithOffset()
    {
        var data = new byte[8];
        ByteHelper.WriteUInt32(data, 2, 0xAABBCCDD);
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x00, data[1]);
        Assert.Equal(0xDD, data[2]);
        Assert.Equal(0xCC, data[3]);
        Assert.Equal(0xBB, data[4]);
        Assert.Equal(0xAA, data[5]);
    }

    #endregion

    #region 字符串方法

    [Fact]
    public void ReadNullTerminatedString_Normal()
    {
        var data = "Hello\0World"u8.ToArray();
        Assert.Equal("Hello", ByteHelper.ReadNullTerminatedString(data, 0));
    }

    [Fact]
    public void ReadNullTerminatedString_WithOffset()
    {
        var data = "XXHello\0"u8.ToArray();
        Assert.Equal("Hello", ByteHelper.ReadNullTerminatedString(data, 2));
    }

    [Fact]
    public void ReadNullTerminatedString_NoNullTerminator()
    {
        var data = "Hello"u8.ToArray();
        Assert.Equal("Hello", ByteHelper.ReadNullTerminatedString(data, 0));
    }

    [Fact]
    public void ReadNullTerminatedString_OffsetOutOfRange_ReturnsEmpty()
    {
        var data = new byte[] { 0x01 };
        Assert.Equal(string.Empty, ByteHelper.ReadNullTerminatedString(data, 5));
    }

    [Fact]
    public void ReadHexString_ReturnsLowercaseHex()
    {
        var data = new byte[] { 0xAB, 0xCD };
        Assert.Equal("abcd", ByteHelper.ReadHexString(data, 0, 2));
    }

    [Fact]
    public void ReadHexStringWithPrefix_ReturnsWithHash()
    {
        var data = new byte[] { 0xAB, 0xCD };
        Assert.Equal("#abcd", ByteHelper.ReadHexStringWithPrefix(data, 0, 2));
    }

    [Fact]
    public void ReadHexString_OutOfRange_ReturnsEmpty()
    {
        var data = new byte[] { 0x01 };
        Assert.Equal(string.Empty, ByteHelper.ReadHexString(data, 0, 5));
    }

    #endregion

    #region 数组操作

    [Fact]
    public void SubArray_ExtractsCorrectSlice()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var result = ByteHelper.SubArray(data, 1, 3);
        Assert.Equal([0x02, 0x03, 0x04], result);
    }

    [Fact]
    public void SubArray_LengthExceedsData_Truncates()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var result = ByteHelper.SubArray(data, 1, 10);
        Assert.Equal([0x02, 0x03], result);
    }

    [Fact]
    public void Substr_IsAliasForSubArray()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var subArray = ByteHelper.SubArray(data, 1, 2);
        var substr = ByteHelper.Substr(data, 1, 2);
        Assert.Equal(subArray, substr);
    }

    [Fact]
    public void StrUpdate_CopiesIntoDest()
    {
        var dest = new byte[6];
        var src = new byte[] { 0xAA, 0xBB, 0xCC };
        ByteHelper.StrUpdate(dest, 2, src);
        Assert.Equal([0x00, 0x00, 0xAA, 0xBB, 0xCC, 0x00], dest);
    }

    [Fact]
    public void RTrim_RemovesTrailingBytes()
    {
        var data = new byte[] { 0x01, 0x02, 0x00, 0x00 };
        var result = ByteHelper.RTrim(data, 0);
        Assert.Equal([0x01, 0x02], result);
    }

    [Fact]
    public void RTrim_AllTrimChar_ReturnsEmpty()
    {
        var data = new byte[] { 0x00, 0x00 };
        var result = ByteHelper.RTrim(data, 0);
        Assert.Empty(result);
    }

    [Fact]
    public void RTrim_NoTrailingTrimChar_ReturnsSame()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var result = ByteHelper.RTrim(data, 0);
        Assert.Equal(data, result);
    }

    #endregion

    #region 数学辅助

    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-1, 0, 10, 0)]
    [InlineData(15, 0, 10, 10)]
    public void IntClamp_ClampsCorrectly(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, ByteHelper.IntClamp(value, min, max));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(16, 16)]
    [InlineData(17, 32)]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    public void IntCeilPow2_ReturnsNextPowerOf2(int value, int expected)
    {
        Assert.Equal(expected, ByteHelper.IntCeilPow2(value));
    }

    [Theory]
    [InlineData(10, 3, 12)]
    [InlineData(9, 3, 9)]
    [InlineData(0, 3, 0)]
    [InlineData(7, 0, 0)]
    public void IntCeil_ReturnsCeilingDivision(int value, int divisor, int expected)
    {
        Assert.Equal(expected, ByteHelper.IntCeil(value, divisor));
    }

    #endregion

    #region 调色板生成

    [Fact]
    public void GrayClut_Returns256Colors()
    {
        var clut = ByteHelper.GrayClut(0x100);
        Assert.Equal(0x100 * 4, clut.Length);
        // First entry: all zeros (black, fully transparent)
        Assert.Equal(0, clut[0]);
        Assert.Equal(0, clut[1]);
        Assert.Equal(0, clut[2]);
        Assert.Equal(0, clut[3]);
        // Last entry: all 255 (white, fully opaque)
        Assert.Equal(255, clut[0x100 * 4 - 4]);
        Assert.Equal(255, clut[0x100 * 4 - 3]);
        Assert.Equal(255, clut[0x100 * 4 - 2]);
        Assert.Equal(255, clut[0x100 * 4 - 1]);
    }

    [Fact]
    public void GradientClut_TwoColors_InterpolatesCorrectly()
    {
        var src = new byte[] { 0, 0, 0, 0 };
        var dst = new byte[] { 100, 100, 100, 100 };
        var clut = ByteHelper.GradientClut(3, src, dst);
        Assert.Equal(12, clut.Length); // 3 * 4
        // Entry 0: (0,0,0,0)
        Assert.Equal(0, clut[0]);
        // Entry 1: (50,50,50,50)
        Assert.Equal(50, clut[4]);
        // Entry 2: (100,100,100,100)
        Assert.Equal(100, clut[8]);
    }

    [Fact]
    public void GradientClut_CountOfOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => ByteHelper.GradientClut(1, [0, 0, 0, 0], [255, 255, 255, 255]));
    }

    #endregion

    #region Round-trip 测试

    [Fact]
    public void WriteReadUInt16_RoundTrip()
    {
        var data = new byte[4];
        ByteHelper.WriteUInt16(data, 1, 0xABCD);
        var result = ByteHelper.ReadUInt16(data, 1);
        Assert.Equal((ushort)0xABCD, result);
    }

    [Fact]
    public void WriteReadUInt32_RoundTrip()
    {
        var data = new byte[8];
        ByteHelper.WriteUInt32(data, 2, 0x12345678);
        var result = ByteHelper.ReadUInt32(data, 2);
        Assert.Equal((uint)0x12345678, result);
    }

    #endregion
}