using System.Threading.Tasks;

namespace QuadToSpine2D.Core.Utility;

public struct Matrix : IEquatable<Matrix>,ICloneable
{
    private int _rows, _cols;
    public int Rows => _rows;
    public int Cols => _cols;
    public double[,] Value { get; }
    public Matrix(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        Value = new double[_rows, _cols];
    }

    public Matrix(int rowsAndCols, bool isIdentity = true)
    {
        _rows = rowsAndCols;
        _cols = rowsAndCols;
        Value = new double[_rows, _cols];
        for (var i = 0; i < rowsAndCols; i++)
        {
            Value[i, i] = 1;
        }
    }
    public Matrix(int rows, int cols,double[] source)
    {
        _rows = rows;
        _cols = cols;
        Value = new double[_rows, _cols];
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                var index = i * cols + j;
                if (index < source.Length)
                    Value[i, j] = source[index];
                else
                    Value[i, j] = 1;
            }
        }
    }    
    public Matrix(int rows, int cols,float[] source)
    {
        _rows = rows;
        _cols = cols;
        Value = new double[_rows, _cols];
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                var index = i * cols + j;
                if (index < source.Length)
                    Value[i, j] = source[index];
                else
                    Value[i, j] = 1;
            }
        }
    }

    public static Matrix IdentityMatrixBy4X4 => new Matrix(4);
    public static Matrix IdentityMatrixBy3X3 => new Matrix(3);
    /// <summary>
    /// Lerp matrix between two matrices
    /// </summary>
    /// <exception cref="Exception">Non-conformable matrices in MatrixProduct</exception>
    public static Matrix Lerp(Matrix srcMatrix, Matrix dstMatrix, double rate)
    {
        if (srcMatrix.Cols != dstMatrix.Cols || srcMatrix.Rows != dstMatrix.Rows)
            throw new Exception("Non-conformable matrices in MatrixProduct");
        return srcMatrix * (1 - rate) + dstMatrix * rate;
    }
    public float[] ToFloats()
    {
        var floats = new float[Rows * Cols];
        var col = Cols;
        var value = Value;
        for (int i = 0; i < _rows; i++)
        {
            for (int j = 0; j < col; j++)
            {
                floats[i + j] = (float)value[i, j];
            }
        }
        return floats;
    }
    public double[] ToDoubles()
    {
        var floats = new double[Rows * Cols];
        var col = Cols;
        var value = Value;
        for (int i = 0; i < _rows; i++)
        {
            for (int j = 0; j < col; j++)
            {
                floats[i + j] = (float)value[i, j];
            }
        }
        return floats;
    }
    public override string ToString()
    {
        var result = string.Empty;
        result += "[ \n";
        for (var i = 0; i < Rows; i++)
        {
            result += "[";
            for (var j = 0; j < Cols; j++)
            {
                result += $"{Value[i, j]}, ";
            }
            result = result.Remove(result.Length - 2);
            result += "] \n";
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
                    result[i,j] += matrixA[i,k] * matrixB[k,j];
            }
        );
        return result;
    }
    public static Matrix operator *(Matrix matrixA, double value)
    {
        for (var i = 0; i < matrixA.Rows; i++)
        {
            for (var j = 0; j < matrixA.Cols; j++)
            {
                matrixA[i, j] *= value;
            }
        }
        return matrixA;
    }
    public static bool operator ==(Matrix value1, Matrix value2) => value1.Equals(value2);
    public static bool operator !=(Matrix value1, Matrix value2) => !value1.Equals(value2);

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
        {
            for (var j = 0; j < value1.Cols; j++)
            {
               result[i,j] = value1[i, j] + value2[i,j];
            }
        }
        return result;
    }
    public static Matrix operator +(Matrix value1, double value2)
    {
        var aRows = value1.Rows; 
        var aCols = value1.Cols;
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        {
            for (var j = 0; j < value1.Cols; j++)
            {
                result[i,j] = value1[i, j] + value2;
            }
        }
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
        {
            for (var j = 0; j < value1.Cols; j++)
            {
                result[i,j] = value1[i, j] - value2[i,j];
            }
        }
        return result;
    }
    public static Matrix operator -(Matrix value1, double value2)
    {
        var aRows = value1.Rows; 
        var aCols = value1.Cols;
        var result = new Matrix(aRows, aCols);
        for (var i = 0; i < value1.Rows; i++)
        {
            for (var j = 0; j < value1.Cols; j++)
            {
                result[i,j] = value1[i, j] - value2;
            }
        }
        return result;
    }
    
    public double this[int row, int col]
    {
        get => Value[row, col];
        set => Value[row, col] = value;
    }
    public bool Equals(Matrix other)
    {
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_rows, _cols, Value);
    }

    public object Clone()
    {
        return Value.Clone();
    }
}