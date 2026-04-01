namespace VanillawareConverter.Mbs.Converters;

public struct Matrix4x4
{
    public double[] M { get; }

    public Matrix4x4()
    {
        M = new double[16];
        M[0] = M[5] = M[10] = M[15] = 1.0;
    }

    public Matrix4x4(double[] values)
    {
        if (values.Length != 16)
            throw new ArgumentException("Matrix4x4 requires exactly 16 values");
        M = values;
    }

    public double this[int index]
    {
        get => M[index];
        set => M[index] = value;
    }

    public static Matrix4x4 Identity()
    {
        return new Matrix4x4();
    }

    public static Matrix4x4 Scale(double sx, double sy, double sz = 1.0)
    {
        var m = Identity();
        m[0] = sx;
        m[5] = sy;
        m[10] = sz;
        return m;
    }

    public static Matrix4x4 RotateX(double radian)
    {
        var m = Identity();
        var sin = System.Math.Sin(radian);
        var cos = System.Math.Cos(radian);
        m[5] = cos;
        m[6] = -sin;
        m[9] = sin;
        m[10] = cos;
        return m;
    }

    public static Matrix4x4 RotateY(double radian)
    {
        var m = Identity();
        var sin = System.Math.Sin(radian);
        var cos = System.Math.Cos(radian);
        m[0] = cos;
        m[2] = sin;
        m[8] = -sin;
        m[10] = cos;
        return m;
    }

    public static Matrix4x4 RotateZ(double radian)
    {
        var m = Identity();
        var sin = System.Math.Sin(radian);
        var cos = System.Math.Cos(radian);
        m[0] = cos;
        m[1] = -sin;
        m[4] = sin;
        m[5] = cos;
        return m;
    }

    public static Matrix4x4 Translate(double tx, double ty, double tz)
    {
        var m = Identity();
        m[3] = tx;
        m[7] = ty;
        m[11] = tz;
        return m;
    }

    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
    {
        var result = new double[16];

        for (var row = 0; row < 4; row++)
        for (var col = 0; col < 4; col++)
        {
            double sum = 0;
            for (var k = 0; k < 4; k++) sum += a[row * 4 + k] * b[k * 4 + col];
            result[row * 4 + col] = sum;
        }

        return new Matrix4x4(result);
    }

    public bool IsIdentity()
    {
        return M[0] == 1 && M[5] == 1 && M[10] == 1 && M[15] == 1 &&
               M[1] == 0 && M[2] == 0 && M[3] == 0 &&
               M[4] == 0 && M[6] == 0 && M[7] == 0 &&
               M[8] == 0 && M[9] == 0 && M[11] == 0 &&
               M[12] == 0 && M[13] == 0 && M[14] == 0;
    }

    public double[] ToArray()
    {
        return (double[])M.Clone();
    }
}