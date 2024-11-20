using System.Threading.Tasks;

namespace QuadToSpine2D.Core.Utility;

public readonly struct Matrix : IEquatable<Matrix>
{
    public int Rows { get; }

    public int Cols { get; }

    public float[,] Value { get; }

    public Matrix(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        Value = new float[Rows, Cols];
    }

    public Matrix(int rowsAndCols, bool isIdentity = true)
    {
        Rows = rowsAndCols;
        Cols = rowsAndCols;
        Value = new float[Rows, Cols];
        for (var i = 0; i < rowsAndCols; i++) Value[i, i] = 1;
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
            if (index < source.Length)
                Value[i, j] = source[index];
            else
                Value[i, j] = 1;
        }
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
        var result = string.Empty;
        result += "[ ";
        for (var i = 0; i < Rows; i++)
        {
            result += "[";
            for (var j = 0; j < Cols; j++) result += $"{Value[i, j]}, ";
            result = result.Remove(result.Length - 2);
            result += "] ";
        }

        result += "]";

        return result;
    }

    public static Matrix operator *(Matrix matrixA, Matrix matrixB)
    {
        var aRows = matrixA.Rows;
        var aCols = matrixA.Cols;
        var bRows = matrixB.Rows;
        var bCols = matrixB.Cols;

        if (aCols != bRows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        var result = new Matrix(aRows, bCols);
        Parallel.For(0, aRows, i =>
            {
                for (var j = 0; j < bCols; ++j)
                for (var k = 0; k < aCols; ++k)
                    result[i, j] += matrixA[i, k] * matrixB[k, j];
            }
        );
        return result;
    }

    public static Matrix operator *(Matrix matrixA, float value)
    {
        for (var i = 0; i < matrixA.Rows; i++)
        for (var j = 0; j < matrixA.Cols; j++)
            matrixA[i, j] *= value;

        return matrixA;
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
        var aRows = value1.Rows;
        var aCols = value1.Cols;
        var bRows = value2.Rows;
        var bCols = value2.Cols;
        if (aCols != bCols || aRows != bRows)
            throw new Exception("Non-conformable matrices in MatrixProduct");
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result[i, j] = value1[i, j] + value2[i, j];

        return result;
    }

    public static Matrix operator +(Matrix value1, float value2)
    {
        var aRows = value1.Rows;
        var aCols = value1.Cols;
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result[i, j] = value1[i, j] + value2;

        return result;
    }

    public static Matrix operator -(Matrix value1, Matrix value2)
    {
        var aRows = value1.Rows;
        var aCols = value1.Cols;
        var bRows = value2.Rows;
        var bCols = value2.Cols;
        if (aCols != bCols || aRows != bRows)
            throw new Exception("Non-conformable matrices in MatrixProduct");
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result[i, j] = value1[i, j] - value2[i, j];

        return result;
    }

    public static Matrix operator -(Matrix value1, float value2)
    {
        var aRows = value1.Rows;
        var aCols = value1.Cols;
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        for (var j = 0; j < value1.Cols; j++)
            result[i, j] = value1[i, j] - value2;

        return result;
    }

    public float this[int row, int col]
    {
        get => Value[row, col];
        set => Value[row, col] = value;
    }

    public bool Equals(Matrix other)
    {
        if (Rows != other.Rows || Cols != other.Cols)
            return false;
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Cols; j++)
            if (ProcessUtility.ApproximatelyEqual(Value[i, j], other.Value[i, j]))
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