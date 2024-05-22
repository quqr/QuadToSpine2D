namespace QuadPlayer;

public static class ProcessTools
{
    public static float[] FindMinAndMaxPoints(float[]? quad)
    {
        if (quad is null) return new float[4];
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < quad.Length; i++)
        {
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
        }

        return [minX, minY, maxX, maxY];
    }
    public static float[] MinusFloats(float[] a, float[] b)
    {
        var c = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            if(i>b.Length) break;
            c[i] = a[i] - b[i];
        }
        return c;
    }
}