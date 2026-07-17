using QTSCore.Utility;

namespace Tests;

public class MatrixTests
{
    #region ToFloatArray

    [Fact]
    public void ToFloatArray_ReturnsRowMajorLayout()
    {
        var m = new Matrix(2, 3,
        [
            1, 2, 3,
            4, 5, 6
        ]);
        var arr = m.ToFloatArray();
        Assert.Equal([1, 2, 3, 4, 5, 6], arr);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var original = new Matrix(2, 2, [1, 2, 3, 4]);
        var clone = (Matrix)original.Clone();
        Assert.Equal(original, clone);
        // Modify clone, original should be unchanged
        clone.Value[0, 0] = 99;
        Assert.Equal(1, original.Value[0, 0]);
    }

    #endregion

    #region 索引器

    [Fact]
    public void Indexer_ReadWrite()
    {
        var m = new Matrix(2, 2);
        Assert.Equal(0, m[0, 0]);
        var m2 = new Matrix(2, 2) { [0, 0] = 42 };
        Assert.Equal(42, m2[0, 0]);
    }

    #endregion

    #region 构造函数

    [Fact]
    public void Constructor_RowCol_CreatesZeroMatrix()
    {
        var m = new Matrix(2, 3);
        Assert.Equal(2, m.Rows);
        Assert.Equal(3, m.Cols);
        Assert.All(m.ToFloatArray(), v => Assert.Equal(0, v));
    }

    [Fact]
    public void Constructor_Square_CreatesIdentityMatrix()
    {
        var m = new Matrix(3);
        Assert.Equal(3, m.Rows);
        Assert.Equal(3, m.Cols);
        // Diagonal should be 1, rest 0
        Assert.Equal(1, m.Value[0, 0]);
        Assert.Equal(0, m.Value[0, 1]);
        Assert.Equal(0, m.Value[1, 0]);
        Assert.Equal(1, m.Value[1, 1]);
        Assert.Equal(1, m.Value[2, 2]);
    }

    [Fact]
    public void Constructor_FromArray_FillsCorrectly()
    {
        var m = new Matrix(2, 2, [1, 2, 3, 4]);
        Assert.Equal(1, m.Value[0, 0]);
        Assert.Equal(2, m.Value[0, 1]);
        Assert.Equal(3, m.Value[1, 0]);
        Assert.Equal(4, m.Value[1, 1]);
    }

    [Fact]
    public void Constructor_FromArray_ShorterArray_PadsWithZero()
    {
        var m = new Matrix(2, 2, [1, 2]);
        Assert.Equal(1, m.Value[0, 0]);
        Assert.Equal(2, m.Value[0, 1]);
        Assert.Equal(0, m.Value[1, 0]);
        Assert.Equal(0, m.Value[1, 1]);
    }

    [Fact]
    public void IdentityMatrixBy4X4_Is4x4Identity()
    {
        var id = Matrix.IdentityMatrixBy4X4;
        Assert.Equal(4, id.Rows);
        Assert.Equal(4, id.Cols);
        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 4; j++)
            Assert.Equal(i == j ? 1 : 0, id.Value[i, j]);
    }

    #endregion

    #region 运算符

    [Fact]
    public void Multiply_4x4By4x4_ReturnsCorrectResult()
    {
        var a = new Matrix(4, 4,
        [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            2, 3, 0, 1
        ]);
        var b = new Matrix(4, 4,
        [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        ]);
        var result = a * b;
        // Identity multiplication
        Assert.Equal(a.Value[3, 0], result.Value[3, 0]);
        Assert.Equal(a.Value[3, 1], result.Value[3, 1]);
    }

    [Fact]
    public void Multiply_4x4By4x2_UsesOptimizedPath()
    {
        var a = new Matrix(4, 4,
        [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            5, 10, 0, 1
        ]);
        var b = new Matrix(4, 2,
        [
            3, 0,
            0, 4,
            0, 0,
            1, 1
        ]);
        var result = a * b;
        Assert.Equal(4, result.Rows);
        Assert.Equal(2, result.Cols);
        Assert.Equal(3, result.Value[0, 0]);
        Assert.Equal(4, result.Value[1, 1]);
        Assert.Equal(16, result.Value[3, 0]); // 5*3 + 10*0 + 0*0 + 1*1
        Assert.Equal(41, result.Value[3, 1]); // 5*0 + 10*4 + 0*0 + 1*1
    }

    [Fact]
    public void Multiply_NonConformable_Throws()
    {
        var a = new Matrix(2, 3);
        var b = new Matrix(2, 2);
        Assert.Throws<Exception>(() => a * b);
    }

    [Fact]
    public void Multiply_Scalar_ReturnsScaledMatrix()
    {
        var m = new Matrix(2, 2, [1, 2, 3, 4]);
        var result = m * 2;
        Assert.Equal([2, 4, 6, 8], result.ToFloatArray());
    }

    [Fact]
    public void Multiply_ScalarCommutative()
    {
        var m = new Matrix(2, 2, [1, 2, 3, 4]);
        var r1 = m * 3;
        var r2 = 3 * m;
        Assert.Equal(r1.ToFloatArray(), r2.ToFloatArray());
    }

    [Fact]
    public void Add_TwoMatrices()
    {
        var a = new Matrix(2, 2, [1, 2, 3, 4]);
        var b = new Matrix(2, 2, [5, 6, 7, 8]);
        var result = a + b;
        Assert.Equal([6, 8, 10, 12], result.ToFloatArray());
    }

    [Fact]
    public void Add_NonConformable_Throws()
    {
        var a = new Matrix(2, 2);
        var b = new Matrix(3, 3);
        Assert.Throws<Exception>(() => a + b);
    }

    [Fact]
    public void Add_Scalar()
    {
        var m = new Matrix(2, 2, [1, 2, 3, 4]);
        var result = m + 10;
        Assert.Equal([11, 12, 13, 14], result.ToFloatArray());
    }

    [Fact]
    public void Subtract_TwoMatrices()
    {
        var a = new Matrix(2, 2, [5, 6, 7, 8]);
        var b = new Matrix(2, 2, [1, 2, 3, 4]);
        var result = a - b;
        Assert.Equal([4, 4, 4, 4], result.ToFloatArray());
    }

    [Fact]
    public void Subtract_Scalar()
    {
        var m = new Matrix(2, 2, [10, 20, 30, 40]);
        var result = m - 5;
        Assert.Equal([5, 15, 25, 35], result.ToFloatArray());
    }

    #endregion

    #region Lerp

    [Fact]
    public void Lerp_AtZero_ReturnsSource()
    {
        var src = new Matrix(2, 2, [0, 0, 0, 0]);
        var dst = new Matrix(2, 2, [10, 10, 10, 10]);
        var result = Matrix.Lerp(src, dst, 0f);
        Assert.Equal(src, result);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsDestination()
    {
        var src = new Matrix(2, 2, [0, 0, 0, 0]);
        var dst = new Matrix(2, 2, [10, 10, 10, 10]);
        var result = Matrix.Lerp(src, dst, 1f);
        Assert.Equal(dst, result);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var src = new Matrix(2, 2, [0, 0, 0, 0]);
        var dst = new Matrix(2, 2, [10, 10, 10, 10]);
        var result = Matrix.Lerp(src, dst, 0.5f);
        var expected = new Matrix(2, 2, [5, 5, 5, 5]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Lerp_NonConformable_Throws()
    {
        var src = new Matrix(2, 2);
        var dst = new Matrix(3, 3);
        Assert.Throws<Exception>(() => Matrix.Lerp(src, dst, 0.5f));
    }

    #endregion

    #region Equals

    [Fact]
    public void Equals_SameMatrix_ReturnsTrue()
    {
        var a = new Matrix(2, 2, [1, 2, 3, 4]);
        var b = new Matrix(2, 2, [1, 2, 3, 4]);
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_DifferentMatrix_ReturnsFalse()
    {
        var a = new Matrix(2, 2, [1, 2, 3, 4]);
        var b = new Matrix(2, 2, [1, 2, 3, 5]);
        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_DifferentDimensions_ReturnsFalse()
    {
        var a = new Matrix(2, 2);
        var b = new Matrix(2, 3);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void GetHashCode_SameMatrices_SameHash()
    {
        // 修复后：GetHashCode 逐元素计算，值相等的矩阵哈希也相等
        var a = new Matrix(2, 2, [1, 2, 3, 4]);
        var b = new Matrix(2, 2, [1, 2, 3, 4]);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_DifferentHash()
    {
        var a = new Matrix(2, 2, [1, 2, 3, 4]);
        var b = new Matrix(2, 2, [1, 2, 3, 5]);
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentDimensions_DifferentHash()
    {
        var a = new Matrix(2, 2);
        var b = new Matrix(3, 3);
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Regression_ValueEquality()
    {
        // 原始 bug：GetHashCode 使用 HashCode.Combine(Rows, Cols, Value)，
        // 其中 Value 是 float[,] 引用，导致内容相同的 Matrix 产生不同哈希。
        // 已修复为逐元素计算哈希。
        var matrices = Enumerable.Range(0, 3)
            .Select(_ => new Matrix(2, 2, [1, 2, 3, 4]))
            .ToList();
        var hashSet = new HashSet<int>(matrices.Select(m => m.GetHashCode()));
        // 所有相同内容的矩阵应有相同的哈希
        Assert.Single(hashSet);
    }

    #endregion

    #region Multiply4X4By4X2Optimized

    [Fact]
    public void Multiply4X4By4X2Optimized_InvalidDimensions_Throws()
    {
        var a = new Matrix(3, 3);
        var b = new Matrix(3, 2);
        Assert.Throws<ArgumentException>(() => Matrix.Multiply4X4By4X2Optimized(a, b));
    }

    [Fact]
    public void Multiply4X4By4X2Optimized_IdentityMatrix()
    {
        var id = new Matrix(4); // Identity
        var b = new Matrix(4, 2,
        [
            1, 2,
            3, 4,
            5, 6,
            7, 8
        ]);
        var result = Matrix.Multiply4X4By4X2Optimized(id, b);
        Assert.Equal(b.ToFloatArray(), result.ToFloatArray());
    }

    #endregion
}