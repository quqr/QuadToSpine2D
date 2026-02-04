namespace QTSCore.Utility;

public static class AnimationMatrixUtility
{
    public static Matrix GetPerspectiveQuad(Matrix dstMatrix)
    {
        var h = PerspectiveMatrix3X3(dstMatrix);
        float[] hInv =
        [
            0, 0, 0.005f,
            -0.001f, 0, 0.015f,
            0, 0.001f, -0.015f
        ];
        var hInvMatrix = new Matrix(3, 3, hInv);
        var m3         = h * hInvMatrix;

        Matrix[] matrices =
        [
            m3 * new Matrix(3, 1, [10, 10, 1]),
            m3 * new Matrix(3, 1, [20, 10, 1]),
            m3 * new Matrix(3, 1, [20, 20, 1]),
            m3 * new Matrix(3, 1, [10, 20, 1])
        ];

        List<float> t  = [];
        var         t1 = matrices[0].ToFloatArray();
        var         t2 = matrices[1].ToFloatArray();
        var         t3 = matrices[2].ToFloatArray();
        var         t4 = matrices[3].ToFloatArray();
        t.AddRange(t1[..2]);
        t.AddRange(t2[..2]);
        t.AddRange(t3[..2]);
        t.AddRange(t4[..2]);
        var result = new Matrix(4, 2, t.ToArray());

        return result;
    }

    private static Matrix PerspectiveMatrix3X3(Matrix dstMatrix)
    {
        var v0 = new Vector3(dstMatrix[0, 0], dstMatrix[0, 1], 1);
        var v1 = new Vector3(dstMatrix[1, 0], dstMatrix[1, 1], 1);
        var v2 = new Vector3(dstMatrix[2, 0], dstMatrix[2, 1], 1);
        var v3 = new Vector3(dstMatrix[3, 0], dstMatrix[3, 1], 1);

        var c0 = Vector3.Cross(Vector3.Cross(v0, v2), Vector3.Cross(v1, v3));
        var c1 = Vector3.Cross(Vector3.Cross(v0, v1), Vector3.Cross(v3, v2));
        var c2 = Vector3.Cross(Vector3.Cross(v0, v3), Vector3.Cross(v1, v2));

        var m = new Matrix(3, 3)
        {
            [0, 0] = c0.X,
            [0, 1] = c1.X,
            [0, 2] = c2.X,
            [1, 0] = c0.Y,
            [1, 1] = c1.Y,
            [1, 2] = c2.Y,
            [2, 0] = c0.Z,
            [2, 1] = c1.Z,
            [2, 2] = c2.Z
        };
        return m;
    }

    /// <summary>
    ///     Multiplies two matrices representing a quad.
    /// </summary>
    public static Matrix QuadMultiply(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA == Matrix.IdentityMatrixBy4X4) return matrixB;
        var result = new Matrix(4, 2);
        for (var i = 0; i < 4; i++)
        {
            result[i, 0] =
                matrixA[0, 0] * matrixB[i, 0]                                 + matrixA[0, 1] * matrixB[i, 1]
                                                                              + matrixA[0, 2] + matrixA[0, 3];
            result[i, 1] =
                matrixA[1, 0] * matrixB[i, 0]                                 + matrixA[1, 1] * matrixB[i, 1]
                                                                              + matrixA[1, 2] + matrixA[1, 3];
        }

        return result;
    }
}