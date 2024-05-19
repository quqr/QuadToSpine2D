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
    public static float[] FindMinAndMaxPoints(float[] old_quad,float[]? new_quad)
    {
        if (new_quad is null) return old_quad;
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        var list = new List<float>(12);
        list.AddRange(old_quad);
        list.AddRange(new_quad);
        for (int i = 4; i < list.Count; i++)
        {
            if (i % 2 == 0)
            {
                minX = float.Min(minX, list[i]);
                maxX = float.Max(maxX, list[i]);
            }
            else
            {
                minY = float.Min(minY, list[i]);
                maxY = float.Max(maxY, list[i]);
            }
        }
        return [minX, minY, maxX, maxY];
    }
}