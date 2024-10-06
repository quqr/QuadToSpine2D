using QuadToSpine2D.Core.Process;
using SixLabors.ImageSharp;

namespace QuadToSpine2D.Core.Utility;

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
        var c                                   = new float[a.Length];
        for (var i = 0; i < a.Length; i++) c[i] = a[i] * b;
        return c;
    }
    
    /// <summary>
    /// Combine animations into one animation data.
    /// new animation data = animation 1 + animation 2 + animation 3 + ...
    /// </summary>
    public static AnimationData CombineAnimations(List<Animation> animations)
    {
        var newAnimation = new AnimationData { Name = "AnimationCombine_" };
        var endFrame     = animations.Select(x => x.Timeline[^1].EndFrame);
        var gcd          = LCM(endFrame);
        foreach (var animation in animations)
        {
            newAnimation.Name   += $"{animation.Name.Last()}_";
            newAnimation.IsLoop =  animation.IsLoop | newAnimation.IsLoop;

            foreach (var timeline in animation.Timeline)
            {
                newAnimation.IsMix = timeline.IsKeyframeMix | timeline.IsMatrixMix | newAnimation.IsMix;
                SetAttachmentsData(newAnimation, timeline, timeline.FramePoint.StartFrame,
                    timeline.FramePoint.EndFrame);
            }

            int times = gcd / (animation.Timeline[^1].EndFrame - animation.Timeline[animation.LoopId].StartFrame);
            var lastTimeline = animation.Timeline.Last();
            for (int i = 1; i <= times; i++)
            {
                for (int j = animation.LoopId; j < animation.Timeline.Count; j++)
                {
                    var newTimeline = lastTimeline.Clone();
                    lastTimeline.Next      = newTimeline;
                    
                    newTimeline.StartFrame = animation.Timeline[j].EndFrame * i;
                    newTimeline.EndFrame   = animation.Timeline[j].EndFrame * i;
                    SetAttachmentsData(newAnimation, newTimeline, newTimeline.StartFrame, newTimeline.EndFrame);
                    lastTimeline = newTimeline;
                }
            }

            newAnimation.Data = newAnimation.Data.OrderBy(x => x.Key).ToDictionary();
        }

        return newAnimation;
    }

    private static void SetAttachmentsData(
        AnimationData newAnimation,
        Timeline timeline,
        int startFrame,
        int endFrame)
    {
        var displayData = GetAttachmentData(newAnimation, startFrame);
        var concealData = GetAttachmentData(newAnimation, endFrame);
        
        if (timeline.Attach is null) return;
        timeline.FramePoint = new FramePoint(startFrame, endFrame);
        
        displayData.DisplayAttachments.Add(timeline);
        concealData.ConcealAttachments.Add(timeline);
        
        // if (animation.IsLoop && frames >= animation.LoopId && endFrame < gcd)
        // {
        //     var nextStartFrame = startFrame * (endFrame / gcd);
        //     var nextEndFrame   = endFrame * (endFrame / gcd);
        //     SetAttachmentsData(animation, newAnimation, newTimeline, frames, gcd, nextStartFrame, nextEndFrame);
        // }
    }

    private static Attachment GetAttachmentData(AnimationData newAnimation, int frame)
    {
        if (!newAnimation.Data.TryGetValue(frame, out var data)) 
            newAnimation.Data.Add(frame, data = new Attachment());
        return data;
    }

    public static Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle
        {
            X      = (int)layer.MinAndMaxSrcPoints[0],
            Y      = (int)layer.MinAndMaxSrcPoints[1],
            Width  = (int)layer.Width,
            Height = (int)layer.Height
        };
    }

    public static bool ApproximatelyEqual(float? a, float? b, float epsilon = 0.01f)
    {
        if (a is null || b is null) return false;
        return Math.Abs((float)(a - b)) < epsilon;
    }
    private static int _GCD(int x, int y)
    {
        return y == 0 ? x : _GCD(y, x % y);
    }
    public static int LCM(IEnumerable<int> nums)
    {
        return nums.Aggregate((x, y) => (x * y) / _GCD(x, y));
    }
}