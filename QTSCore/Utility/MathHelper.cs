namespace QTSCore.Utility;

/// <summary>
/// 数学计算辅助工具，提供坐标和浮点数组操作。
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// 在浮点数组中查找最小和最大坐标点
    /// </summary>
    /// <param name="quad">包含坐标点的浮点数组，格式为[x1,y1,x2,y2,...]</param>
    /// <returns>包含最小和最大坐标的数组：[minX, minY, maxX, maxY]</returns>
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
    /// 计算两个浮点数组的差值
    /// </summary>
    public static float[] MinusFloats(float[]? a, float[]? b)
    {
        if (a is null || b is null) return [];
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            if (i >= b.Length) break;
            c[i] = a[i] - b[i];
        }

        return c;
    }

    /// <summary>
    /// 将浮点数组的每个元素乘以指定标量
    /// </summary>
    public static float[]? MulFloats(float[]? a, float b)
    {
        if (a is null) return null;
        if (ApproximatelyEqual(b, 1f)) return a;
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++) c[i] = a[i] * b;
        return c;
    }

    /// <summary>
    /// 判断两个浮点数是否近似相等
    /// </summary>
    public static bool ApproximatelyEqual(float? a, float? b, float epsilon = 0.000001f)
    {
        if (a is null || b is null) return false;
        return Math.Abs((float)(a - b)) < epsilon;
    }
}
