using QTSCore.Utility;

namespace Tests;

public class MathHelperTests
{
    #region FindMinAndMaxPoints

    [Fact]
    public void FindMinAndMaxPoints_NormalInput()
    {
        float[] quad = [0, 0, 10, 0, 10, 5, 0, 5];
        var result = MathHelper.FindMinAndMaxPoints(quad);
        Assert.Equal([0, 0, 10, 5], result);
    }

    [Fact]
    public void FindMinAndMaxPoints_NegativeCoordinates()
    {
        float[] quad = [-5, -3, 5, -3, 5, 7, -5, 7];
        var result = MathHelper.FindMinAndMaxPoints(quad);
        Assert.Equal([-5, -3, 5, 7], result);
    }

    [Fact]
    public void FindMinAndMaxPoints_NullInput_ReturnsZeroArray()
    {
        var result = MathHelper.FindMinAndMaxPoints(null);
        Assert.Equal(new float[4], result);
    }

    [Fact]
    public void FindMinAndMaxPoints_EmptyInput_ReturnsFourElementArray()
    {
        var result = MathHelper.FindMinAndMaxPoints([]);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void FindMinAndMaxPoints_SinglePoint()
    {
        float[] quad = [3, 7];
        var result = MathHelper.FindMinAndMaxPoints(quad);
        Assert.Equal([3, 7, 3, 7], result);
    }

    [Fact]
    public void FindMinAndMaxPoints_FloatPrecision()
    {
        float[] quad = [0.1f, 0.2f, 0.3f, 0.4f];
        var result = MathHelper.FindMinAndMaxPoints(quad);
        Assert.Equal(0.1f, result[0]);
        Assert.Equal(0.2f, result[1]);
        Assert.Equal(0.3f, result[2]);
        Assert.Equal(0.4f, result[3]);
    }

    #endregion

    #region MinusFloats

    [Fact]
    public void MinusFloats_NormalSubtraction()
    {
        var a = new float[] { 10, 20, 30 };
        var b = new float[] { 1, 2, 3 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([9, 18, 27], result);
    }

    [Fact]
    public void MinusFloats_DifferentLengths_TruncatesToShorter()
    {
        // 修复后：结果长度为 min(a.Length, b.Length)，不再零填充
        var a = new float[] { 10, 20, 30, 40 };
        var b = new float[] { 1, 2 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([9, 18], result);
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void MinusFloats_bLongerThanA_TruncatesToALength()
    {
        var a = new float[] { 10, 20 };
        var b = new float[] { 1, 2, 3, 4 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([9, 18], result);
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void MinusFloats_NullA_ReturnsEmpty()
    {
        var result = MathHelper.MinusFloats(null, [1, 2]);
        Assert.Empty(result);
    }

    [Fact]
    public void MinusFloats_NullB_ReturnsEmpty()
    {
        var result = MathHelper.MinusFloats([1, 2], null);
        Assert.Empty(result);
    }

    [Fact]
    public void MinusFloats_BothNull_ReturnsEmpty()
    {
        var result = MathHelper.MinusFloats(null, null);
        Assert.Empty(result);
    }

    [Fact]
    public void MinusFloats_EmptyArrays_ReturnsEmpty()
    {
        var result = MathHelper.MinusFloats([], []);
        Assert.Empty(result);
    }

    [Fact]
    public void MinusFloats_SameLength_NoTruncation()
    {
        var a = new float[] { 5, 10, 15 };
        var b = new float[] { 1, 2, 3 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([4, 8, 12], result);
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void MinusFloats_NegativeValues()
    {
        var a = new float[] { -5, 10, -3 };
        var b = new float[] { 3, -2, 7 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([-8, 12, -10], result);
    }

    [Fact]
    public void MinusFloats_Regression_OffByOneFixed()
    {
        // 原始 bug：if (i > b.Length) break; 允许 i == b.Length 时越界访问
        // 已修复为：结果长度 = min(a.Length, b.Length)，不会越界
        var a = new float[] { 5, 10, 15 };
        var b = new float[] { 1, 2 };
        var result = MathHelper.MinusFloats(a, b);
        Assert.Equal([4, 8], result);
        Assert.Equal(2, result.Length);
    }

    #endregion

    #region MulFloats

    [Fact]
    public void MulFloats_NormalMultiplication()
    {
        var a = new float[] { 1, 2, 3 };
        var result = MathHelper.MulFloats(a, 2);
        Assert.Equal([2, 4, 6], result);
    }

    [Fact]
    public void MulFloats_ByOne_ReturnsSameArray()
    {
        var a = new float[] { 1, 2, 3 };
        var result = MathHelper.MulFloats(a, 1f);
        Assert.Same(a, result);
    }

    [Fact]
    public void MulFloats_ByZero_ReturnsZeroArray()
    {
        var a = new float[] { 1, 2, 3 };
        var result = MathHelper.MulFloats(a, 0);
        Assert.Equal([0, 0, 0], result);
    }

    [Fact]
    public void MulFloats_NullInput_ReturnsNull()
    {
        var result = MathHelper.MulFloats(null, 2);
        Assert.Null(result);
    }

    [Fact]
    public void MulFloats_NegativeScalar()
    {
        var a = new float[] { 1, -2, 3 };
        var result = MathHelper.MulFloats(a, -1);
        Assert.Equal([-1, 2, -3], result);
    }

    #endregion

    #region ApproximatelyEqual

    [Fact]
    public void ApproximatelyEqual_SameValues_ReturnsTrue()
    {
        Assert.True(MathHelper.ApproximatelyEqual(1.0f, 1.0f));
    }

    [Fact]
    public void ApproximatelyEqual_CloseValues_ReturnsTrue()
    {
        Assert.True(MathHelper.ApproximatelyEqual(1.0f, 1.0f + 1e-7f));
    }

    [Fact]
    public void ApproximatelyEqual_DistantValues_ReturnsFalse()
    {
        Assert.False(MathHelper.ApproximatelyEqual(1.0f, 2.0f));
    }

    [Fact]
    public void ApproximatelyEqual_NullA_ReturnsFalse()
    {
        Assert.False(MathHelper.ApproximatelyEqual(null, 1.0f));
    }

    [Fact]
    public void ApproximatelyEqual_NullB_ReturnsFalse()
    {
        Assert.False(MathHelper.ApproximatelyEqual(1.0f, null));
    }

    [Fact]
    public void ApproximatelyEqual_BothNull_ReturnsFalse()
    {
        Assert.False(MathHelper.ApproximatelyEqual(null, null));
    }

    [Fact]
    public void ApproximatelyEqual_CustomEpsilon()
    {
        Assert.True(MathHelper.ApproximatelyEqual(1.0f, 1.01f, 0.1f));
        Assert.False(MathHelper.ApproximatelyEqual(1.0f, 1.01f, 0.001f));
    }

    [Fact]
    public void ApproximatelyEqual_ZeroAndNearZero()
    {
        Assert.True(MathHelper.ApproximatelyEqual(0f, 1e-7f));
        Assert.False(MathHelper.ApproximatelyEqual(0f, 0.001f));
    }

    #endregion
}