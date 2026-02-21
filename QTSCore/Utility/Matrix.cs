namespace QTSCore.Utility;

public readonly struct Matrix : IEquatable<Matrix>, ICloneable
{
    public int Rows { get; }

    public int Cols { get; }

    public float[,] Value { get; init; }

    public Matrix(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        Value = new float[Rows, Cols];
    }

    public Matrix(int rowsAndCols)
    {
        Rows = rowsAndCols;
        Cols = rowsAndCols;
        Value = new float[Rows, Cols];
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Cols; j++)
            Value[i, j] = i == j ? 1 : 0;
    }

    public Matrix(int rows, int cols, float[] source)
    {
        Rows = rows;
        Cols = cols;
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
            result = result.Remove(result.Length - 2);
            result += "] ";
        }

        result += "]";

        return result;
    }

    public object Clone()
    {
        var clonedValues = new float[Rows, Cols];
        Array.Copy(Value, clonedValues, Value.Length);
        return new Matrix(Rows, Cols) { Value = clonedValues };
    }

    public static Matrix operator *(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA.Cols != matrixB.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        var result = new Matrix(matrixA.Rows, matrixB.Cols);
        Parallel.For(0, matrixA.Rows, i =>
            {
                for (var j = 0; j < matrixB.Cols; ++j)
                for (var k = 0; k < matrixA.Cols; ++k)
                    result.Value[i, j] += matrixA.Value[i, k] * matrixB.Value[k, j];
            }
        );
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