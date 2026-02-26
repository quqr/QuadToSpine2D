namespace QTSCore.Utility;

public readonly struct Matrix : IEquatable<Matrix>, ICloneable
{
    public int Rows { get; }

    public int Cols { get; }

    public float[,] Value { get; init; }

    public Matrix(int rows, int cols)
    {
        Rows  = rows;
        Cols  = cols;
        Value = new float[Rows, Cols];
    }

    public Matrix(int rowsAndCols)
    {
        Rows  = rowsAndCols;
        Cols  = rowsAndCols;
        Value = new float[Rows, Cols];
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Cols; j++)
            Value[i, j] = i == j ? 1 : 0;
    }

    public Matrix(int rows, int cols, float[] source)
    {
        Rows  = rows;
        Cols  = cols;
        Value = new float[Rows, Cols];
        for (var i = 0; i < rows; i++)
        for (var j = 0; j < cols; j++)
        {
            var index = i * cols + j;
            Value[i, j] = index < source.Length ? source[index] : 0;
        }
    }

    public bool IsZeroMatrix()
    {
        const float epsilon = 1e-6f;
        return Value.Cast<float>().All(f => MathF.Abs(f) < epsilon);
    }

    public static Matrix IdentityMatrixBy4X4 => new(4);

    /// <summary>
    ///     Lerp matrix between two matrices
    /// </summary>
    /// <exception cref="Exception">Non-conformable matrices in MatrixProduct</exception>
    public static Matrix Lerp(Matrix srcMatrix, Matrix dstMatrix, float rate)
    {
        if (srcMatrix.Cols != dstMatrix.Cols || srcMatrix.Rows != dstMatrix.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");
        return srcMatrix * (1 - rate) + dstMatrix * rate;
    }

    public float[] ToFloatArray()
    {
        var array = new float[Rows * Cols];
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Cols; j++)
            array[j + i * Cols] = Value[i, j];

        return array;
    }

    public override string ToString()
    {
        var result = "[ ";
        for (var i = 0; i < Rows; i++)
        {
            result += "[";
            for (var j = 0; j < Cols; j++)
                result += $"{Value[i, j]}, ";
            result =  result.Remove(result.Length - 2);
            result += "] ";
        }

        result += "]";

        return result;
    }

    public object Clone()
    {
        var clonedValues = new float[Rows, Cols];
        Array.Copy(Value, clonedValues, Value.Length);
        return new Matrix(Rows, Cols)
        {
            Value = clonedValues
        };
    }

    public static Matrix operator *(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA.Cols != matrixB.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        var result = new Matrix(matrixA.Rows, matrixB.Cols);
        var colsB  = matrixB.Cols;
        var colsA  = matrixA.Cols;
        switch (matrixA)
        {
            case { Rows: 4, Cols: 4 } when matrixB is { Rows: 4, Cols: 2 }:
                return Multiply4x4By4x2Optimized(matrixA, matrixB);
            case { Rows: 4, Cols: 4 } when matrixB is { Rows: 4, Cols: 4 }:
                Multiply4X4(matrixA.Value, matrixB.Value, result.Value);
                return result;
        }

        var simdWidth = Vector<float>.Count; // 通常为 4 或 8
        Parallel.For(0, matrixA.Rows, i =>
        {
            for (var j = 0; j < colsB; j++)
            {
                float sum = 0;
                var   k   = 0;

                // SIMD 向量化循环
                for (; k <= colsA - simdWidth; k += simdWidth)
                {
                    var vectorA = new Vector<float>(GetRow(matrixA, i, k, simdWidth));
                    var vectorB = new Vector<float>(GetCol(matrixB, k, j, simdWidth));
                    sum += Vector.Dot(vectorA, vectorB);
                }

                // 处理剩余元素
                for (; k < colsA; k++)
                    sum += matrixA.Value[i, k] * matrixB.Value[k, j];

                result.Value[i, j] = sum;
            }
        });

        return result;
    }

    private static float[] GetRow(Matrix matrix, int row, int startCol, int length)
    {
        var data = new float[length];
        for (var i = 0; i < length && startCol + i < matrix.Cols; i++)
            data[i] = matrix.Value[row, startCol + i];
        return data;
    }

    private static float[] GetCol(Matrix matrix, int startRow, int col, int length)
    {
        var data = new float[length];
        for (var i = 0; i < length && startRow + i < matrix.Rows; i++)
            data[i] = matrix.Value[startRow + i, col];
        return data;
    }

// 4x4矩阵完全展开的乘法
    private static void Multiply4X4(float[,] a, float[,] b, float[,] c)
    {
        // 第0行
        c[0, 0] = a[0, 0] * b[0, 0] + a[0, 1] * b[1, 0] + a[0, 2] * b[2, 0] + a[0, 3] * b[3, 0];
        c[0, 1] = a[0, 0] * b[0, 1] + a[0, 1] * b[1, 1] + a[0, 2] * b[2, 1] + a[0, 3] * b[3, 1];
        c[0, 2] = a[0, 0] * b[0, 2] + a[0, 1] * b[1, 2] + a[0, 2] * b[2, 2] + a[0, 3] * b[3, 2];
        c[0, 3] = a[0, 0] * b[0, 3] + a[0, 1] * b[1, 3] + a[0, 2] * b[2, 3] + a[0, 3] * b[3, 3];

        // 第1行
        c[1, 0] = a[1, 0] * b[0, 0] + a[1, 1] * b[1, 0] + a[1, 2] * b[2, 0] + a[1, 3] * b[3, 0];
        c[1, 1] = a[1, 0] * b[0, 1] + a[1, 1] * b[1, 1] + a[1, 2] * b[2, 1] + a[1, 3] * b[3, 1];
        c[1, 2] = a[1, 0] * b[0, 2] + a[1, 1] * b[1, 2] + a[1, 2] * b[2, 2] + a[1, 3] * b[3, 2];
        c[1, 3] = a[1, 0] * b[0, 3] + a[1, 1] * b[1, 3] + a[1, 2] * b[2, 3] + a[1, 3] * b[3, 3];

        // 第2行
        c[2, 0] = a[2, 0] * b[0, 0] + a[2, 1] * b[1, 0] + a[2, 2] * b[2, 0] + a[2, 3] * b[3, 0];
        c[2, 1] = a[2, 0] * b[0, 1] + a[2, 1] * b[1, 1] + a[2, 2] * b[2, 1] + a[2, 3] * b[3, 1];
        c[2, 2] = a[2, 0] * b[0, 2] + a[2, 1] * b[1, 2] + a[2, 2] * b[2, 2] + a[2, 3] * b[3, 2];
        c[2, 3] = a[2, 0] * b[0, 3] + a[2, 1] * b[1, 3] + a[2, 2] * b[2, 3] + a[2, 3] * b[3, 3];

        // 第3行
        c[3, 0] = a[3, 0] * b[0, 0] + a[3, 1] * b[1, 0] + a[3, 2] * b[2, 0] + a[3, 3] * b[3, 0];
        c[3, 1] = a[3, 0] * b[0, 1] + a[3, 1] * b[1, 1] + a[3, 2] * b[2, 1] + a[3, 3] * b[3, 1];
        c[3, 2] = a[3, 0] * b[0, 2] + a[3, 1] * b[1, 2] + a[3, 2] * b[2, 2] + a[3, 3] * b[3, 2];
        c[3, 3] = a[3, 0] * b[0, 3] + a[3, 1] * b[1, 3] + a[3, 2] * b[2, 3] + a[3, 3] * b[3, 3];
    }
    public static Matrix Multiply4x4By4x2Optimized(Matrix a, Matrix b)
    {
        if (a.Rows != 4 || a.Cols != 4 || b.Rows != 4 || b.Cols != 2)
        {
            throw new ArgumentException("矩阵维度必须为4x4和4x2");
        }
    
        var result = new Matrix(4, 2);
        var aVal   = a.Value;
        var bVal   = b.Value;
        var cVal   = result.Value;
    
        // 预取所有矩阵元素到局部变量，减少数组访问
        float a00 = aVal[0, 0], a01 = aVal[0, 1], a02 = aVal[0, 2], a03 = aVal[0, 3];
        float a10 = aVal[1, 0], a11 = aVal[1, 1], a12 = aVal[1, 2], a13 = aVal[1, 3];
        float a20 = aVal[2, 0], a21 = aVal[2, 1], a22 = aVal[2, 2], a23 = aVal[2, 3];
        float a30 = aVal[3, 0], a31 = aVal[3, 1], a32 = aVal[3, 2], a33 = aVal[3, 3];
    
        float b00 = bVal[0, 0], b01 = bVal[0, 1];
        float b10 = bVal[1, 0], b11 = bVal[1, 1];
        float b20 = bVal[2, 0], b21 = bVal[2, 1];
        float b30 = bVal[3, 0], b31 = bVal[3, 1];
    
        // 完全展开计算
        cVal[0, 0] = a00 * b00 + a01 * b10 + a02 * b20 + a03 * b30;
        cVal[0, 1] = a00 * b01 + a01 * b11 + a02 * b21 + a03 * b31;
    
        cVal[1, 0] = a10 * b00 + a11 * b10 + a12 * b20 + a13 * b30;
        cVal[1, 1] = a10 * b01 + a11 * b11 + a12 * b21 + a13 * b31;
    
        cVal[2, 0] = a20 * b00 + a21 * b10 + a22 * b20 + a23 * b30;
        cVal[2, 1] = a20 * b01 + a21 * b11 + a22 * b21 + a23 * b31;
    
        cVal[3, 0] = a30 * b00 + a31 * b10 + a32 * b20 + a33 * b30;
        cVal[3, 1] = a30 * b01 + a31 * b11 + a32 * b21 + a33 * b31;
    
        return result;
    }

    public static Matrix operator *(Matrix matrixA, float value)
    {
        var result = new Matrix(matrixA.Rows, matrixA.Cols);
        for (var i = 0; i < matrixA.Rows; i++)
        for (var j = 0; j < matrixA.Cols; j++)
            result.Value[i, j] = matrixA.Value[i, j] * value;

        return result;
    }

    public static Matrix operator *(float value, Matrix matrixA)
    {
        return matrixA * value;
    }

    public static bool operator ==(Matrix value1, Matrix value2)
    {
        return value1.Equals(value2);
    }

    public static bool operator !=(Matrix value1, Matrix value2)
    {
        return !value1.Equals(value2);
    }

    public static Matrix operator +(Matrix value1, Matrix value2)
    {
        if (value1.Cols != value2.Cols || value1.Rows != value2.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        var result = new Matrix(value1.Rows, value1.Cols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result.Value[i, j] = value1.Value[i, j] + value2.Value[i, j];

        return result;
    }

    public static Matrix operator +(Matrix value1, float value2)
    {
        var result = new Matrix(value1.Rows, value1.Cols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result.Value[i, j] = value1.Value[i, j] + value2;

        return result;
    }

    public static Matrix operator -(Matrix value1, Matrix value2)
    {
        if (value1.Cols != value2.Cols || value1.Rows != value2.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        var result = new Matrix(value1.Rows, value1.Cols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result.Value[i, j] = value1.Value[i, j] - value2.Value[i, j];

        return result;
    }

    public static Matrix operator -(Matrix value1, float value2)
    {
        var result = new Matrix(value1.Rows, value1.Cols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result.Value[i, j] = value1.Value[i, j] - value2;

        return result;
    }

    public float this[int row, int col]
    {
        get => Value[row, col];
        init => Value[row, col] = value;
    }

    public bool Equals(Matrix other)
    {
        if (Rows != other.Rows || Cols != other.Cols)
            return false;
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Cols; j++)
            if (!ProcessUtility.ApproximatelyEqual(Value[i, j], other.Value[i, j]))
                return false;

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rows, Cols, Value);
    }
}