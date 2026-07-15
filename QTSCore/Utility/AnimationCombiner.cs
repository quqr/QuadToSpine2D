using QTSAvalonia.Helper;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Process;

namespace QTSCore.Utility;

/// <summary>
/// 动画合并工具，将多个 Quad 动画合并为单个动画数据。
/// </summary>
public static class AnimationCombiner
{
    /// <summary>
    /// 将多个动画合并为单个动画数据
    /// </summary>
    /// <param name="animations">要合并的动画列表</param>
    /// <returns>合并后的 AnimationData 实例</returns>
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

    /// <summary>
    /// 按帧设置循环数据
    /// </summary>
    private static void SetLoopDataByFrame(Animation animation, AnimationData newAnimation, int maxFrame)
    {
        if (!animation.IsLoop || animation.Timeline[^1].EndFrame == maxFrame) return;
        SetAttachmentsData(newAnimation, animation.Timeline[^1], animation.Timeline[^1].EndFrame, maxFrame);
    }

    /// <summary>
    /// 设置循环数据
    /// </summary>
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

    /// <summary>
    /// 设置附件数据
    /// </summary>
    private static void SetAttachmentsData(
        AnimationData newAnimation,
        Timeline timeline,
        int startFrame,
        int endFrame)
    {
        if (timeline.Attach is null) return;

        var displayData = GetAttachmentData(newAnimation, startFrame);
        var concealData = GetAttachmentData(newAnimation, endFrame);

        timeline.FramePoint = new FramePoint(startFrame, endFrame);

        displayData.DisplayAttachments.Add(timeline);
        concealData.ConcealAttachments.Add(timeline);
    }

    /// <summary>
    /// 获取或创建指定帧的附件数据
    /// </summary>
    private static Attachment GetAttachmentData(AnimationData newAnimation, int frame)
    {
        if (!newAnimation.Data.TryGetValue(frame, out var data))
            newAnimation.Data.Add(frame, data = new Attachment());
        return data;
    }
}
