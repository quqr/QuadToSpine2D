using QTSAvalonia.Helper;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Process;
using SkiaSharp;

namespace QTSCore.Utility;

public static class ProcessUtility
{
    /// <summary>
    ///     Find min and max point in float[4]
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
    ///     return a - b
    /// </summary>
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

    /// <summary>
    ///     float[] a *  b
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
    ///     Combine animations into one animation data.
    ///     A animation may be contain multiple animations.
    ///     new animation data = animation 1 + animation 2 + animation 3 + ...
    /// </summary>
    public static AnimationData CombineAnimations(List<Animation?> animations)
    {
        var newAnimation = new AnimationData();

        var maxFrame = animations.Max(x => x!.Timeline.Max(y => y.EndFrame));

        foreach (var animation in animations)
        {
            if (animation is null) continue;
            newAnimation.IsLoop = animation.IsLoop | newAnimation.IsLoop;

            foreach (var timeline in animation.Timeline)
            {
                newAnimation.IsMix = timeline.DstquadMixId != -1 | timeline.MatrixMixId != -1 | newAnimation.IsMix;
                SetAttachmentsData(newAnimation, timeline, timeline.StartFrame,
                    timeline.EndFrame);
            }

            if (Instances.ConverterSetting.IsLoopingAnimation) SetLoopData(animation, newAnimation, maxFrame);
            newAnimation.Data = newAnimation.Data.OrderBy(x => x.Key).ToDictionary();
        }

        return newAnimation;
    }

    private static void SetLoopDataByFrame(Animation animation, AnimationData newAnimation, int maxFrame)
    {
        if (!animation.IsLoop || animation.Timeline[^1].EndFrame == maxFrame) return;
        SetAttachmentsData(newAnimation, animation.Timeline[^1], animation.Timeline[^1].EndFrame, maxFrame);
    }

    private static void SetLoopData(Animation animation, AnimationData newAnimation, int maxFrame)
    {
        if (!animation.IsLoop || animation.Timeline[^1].EndFrame == maxFrame) return;
        var lastTimeline = animation.Timeline[^1];
        while (true)
            for (var j = animation.LoopId; j < animation.Timeline.Length; j++)
            {
                var newTimeline = animation.Timeline[animation.LoopId].Clone();

                lastTimeline.Next = newTimeline;
                newTimeline.Prev = lastTimeline;

                if (newTimeline.EndFrame >= maxFrame)
                {
                    newTimeline.EndFrame = maxFrame;
                    SetAttachmentsData(newAnimation, newTimeline, newTimeline.StartFrame, newTimeline.EndFrame);
                    return;
                }

                SetAttachmentsData(newAnimation, newTimeline, newTimeline.StartFrame, newTimeline.EndFrame);
                lastTimeline = newTimeline;
            }
    }

    private static void SetAttachmentsData(
        AnimationData newAnimation,
        Timeline timeline,
        int startFrame,
        int endFrame)
    {
        if (timeline.Attach is null) return; // draw nothing

        var displayData = GetAttachmentData(newAnimation, startFrame);
        var concealData = GetAttachmentData(newAnimation, endFrame);

        timeline.FramePoint = new FramePoint(startFrame, endFrame);

        displayData.DisplayAttachments.Add(timeline);
        concealData.ConcealAttachments.Add(timeline);
    }

    private static Attachment GetAttachmentData(AnimationData newAnimation, int frame)
    {
        if (!newAnimation.Data.TryGetValue(frame, out var data))
            newAnimation.Data.Add(frame, data = new Attachment());
        return data;
    }

    public static SKRectI CalculateRectangle(KeyframeLayer layer)
    {
        return SKRectI.Create((int)layer.MinAndMaxSrcPoints[0], (int)layer.MinAndMaxSrcPoints[1],
            (int)layer.Width, (int)layer.Height);
    }

    public static bool ApproximatelyEqual(float? a, float? b, float epsilon = 0.000001f)
    {
        if (a is null || b is null) return false;
        return Math.Abs((float)(a - b)) < epsilon;
    }
}