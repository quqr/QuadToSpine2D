using System.Threading.Tasks;
using QuadToSpine2D.Core.Process;

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
    /// return a - b
    /// </summary>
    /// <returns>return a - b. if a or b is null return [].</returns>
    public static float[] MinusFloats(float[]? a, float[]? b)
    {
        if (a is null || b is null) return [];
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            if (i > b.Length) break;
            c[i] = a[i] - b[i];
        }

        return c;
    }

    public static double[] MinusDoubles(double[]? a, double[]? b)
    {
        if (a is null || b is null) return [];
        var c = new double[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            if (i > b.Length) break;
            c[i] = a[i] - b[i];
        }

        return c;
    }

    /// <summary>
    /// float[] a *  b
    /// </summary>
    /// <returns>return a *  b, if a is null return null</returns>
    public static float[]? MulFloats(float[]? a, float b)
    {
        if (a is null) return null;
        if (Math.Abs(b - 1) < .1f) return a;
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++) c[i] = a[i] * b;
        return c;
    }
    public static AnimationData CombineAnimations(List<Animation> animations)
    {
        var newAnimation = new AnimationData{Name = "AnimationCombine_"};
        foreach (var animation in animations)
        {
            newAnimation.Name += $"{animation.Name.Last()}_";
            newAnimation.IsLoop = animation.IsLoop | newAnimation.IsLoop;
            foreach (var timeline in animation.Timeline)
            {
                newAnimation.IsMix = timeline.IsKeyframeMix | timeline.IsMatrixMix | newAnimation.IsMix;
                if (!newAnimation.Data.TryGetValue(timeline.StartFrame, out var displayData))
                {
                    displayData = new Attachment();
                    newAnimation.Data[timeline.StartFrame] = displayData;
                }

                if (!newAnimation.Data.TryGetValue(timeline.EndFrame, out var concealData))
                {
                    concealData = new Attachment();
                    newAnimation.Data[timeline.EndFrame] = concealData;
                }
                displayData.DisplayAttachments.Add(timeline);
                concealData.ConcealAttachments.Add(timeline);
            }

            newAnimation.Data = newAnimation.Data.OrderBy(x => x.Key).ToDictionary();
        }

        return newAnimation;
    }
}