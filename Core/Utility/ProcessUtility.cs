namespace QuadToSpine2D.Core.Utility;

public static class ProcessUtility
{
    /// <summary>
    /// Find min and max point in float[4]
    /// </summary>
    /// <param name="quad">If quad is null, return new float[4]</param>
    /// <returns>return the min and max points: [minX, minY, maxX, maxY]</returns>
    public static float[] FindMinAndMaxPoints(float[]? quad)
    {
        if (quad is null) return new float[4];
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (var i = 0; i < quad.Length; i++)
            if (i % 2 == 0)
            {
                minX = float.Min(minX, quad[i]);
                maxX = float.Max(maxX, quad[i]);
            }
            else
            {
                minY = float.Min(minY, quad[i]);
                maxY = float.Max(maxY, quad[i]);
            }

        return [minX, minY, maxX, maxY];
    }
    /// <summary>
    /// float[] a - float[] b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float[] MinusFloats(float[] a, float[] b)
    {
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            if (i > b.Length) break;
            c[i] = a[i] - b[i];
        }

        return c;
    }
    /// <summary>
    /// float[] a * float b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float[]? MulFloats(float[]? a, float b)
    {
        if (a is null) return null;
        if (Math.Abs(b - 1) < .1f) return a;
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++) c[i] = a[i] * b;
        return c;
    }
}