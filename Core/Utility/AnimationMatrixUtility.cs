namespace QuadToSpine2D.Core.Utility;

public static class AnimationMatrixUtility
{
    /// <summary>
    /// I dont know what this is doing, but it seems to be related to perspective projection.
    /// </summary>
    /// <param name="matrix"></param>
    /// <returns></returns>
    public static Matrix GetPerspectiveQuad(Matrix matrix)
    {
        var h = PerspectiveMatrix3X3(matrix);
        double[] hInv = [
            0     , 0     ,  0.005 ,
            -0.001 , 0     ,  0.015 ,
            0     , 0.001 , -0.015 ,
        ];
        var hInvMatrix = new Matrix(3,3,hInv);
        var m3 = h * hInvMatrix;
        
        Matrix[] matrices =
        [
            m3 * new Matrix(1, 2, [10d, 10d]),
            m3 * new Matrix(1, 2, [20d, 10d]),
            m3 * new Matrix(1, 2, [20d, 20d]),
            m3 * new Matrix(1, 2, [10d, 20d])
        ];
        List<double> t = [];
        t.AddRange(matrices[0].ToDoubles());
        t.AddRange(matrices[1].ToDoubles());
        t.AddRange(matrices[2].ToDoubles());
        t.AddRange(matrices[3].ToDoubles());
        return new Matrix(4,2,t.ToArray());
    }

    private static Matrix PerspectiveMatrix3X3(Matrix matrix)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Multiplies two matrices representing a quad.
    /// </summary>
    /// <param name="matrixA"></param>
    /// <param name="matrixB"></param>
    /// <returns></returns>
    public static Matrix QuadMultiply(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA == Matrix.IdentityMatrixBy4X4)
        {
            return matrixB;
        }
        var result = new Matrix(4,2);
        for (var i = 0; i < 4; i++)
        {
            result[i, 0] = 
                matrixA[0, 0] * matrixB[i, 0] + matrixA[0, 1] * matrixB[i, 1] 
                                              + matrixA[0, 2] + matrixA[0, 3];
            result[i, 1] = 
                matrixA[1, 0] * matrixB[i, 0] + matrixA[1, 1] * matrixB[i, 1] 
                                              + matrixA[1, 2] + matrixA[1, 3];
        }
        return result;
    }
}