using QTSAvalonia.Helper;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Process;
using SkiaSharp;

namespace QTSCore.Utility;

/// <summary>
/// 提供处理Quad和Spine数据的通用工具方法
/// </summary>
/// <remarks>
/// 此静态类包含用于坐标计算、动画合并、图像处理等操作的辅助方法。
/// 所有方法都是静态的，可直接调用而无需实例化。
/// </remarks>
public static class ProcessUtility
{
    /// <summary>
    /// 在浮点数组中查找最小和最大坐标点
    /// </summary>
    /// <param name="quad">
    /// 包含坐标点的浮点数组，格式为[x1,y1,x2,y2,...]。
    /// 如果为null，则返回全零数组。
    /// </param>
    /// <returns>
    /// 包含最小和最大坐标的数组：[minX, minY, maxX, maxY]
    /// </returns>
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
    /// <param name="a">被减数数组</param>
    /// <param name="b">减数数组</param>
    /// <returns>
    /// 包含差值的新数组。如果任一参数为null，返回空数组。
    /// 结果数组长度与a相同，超出b长度的部分保持原值。
    /// </returns>
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
    /// 将浮点数组的每个元素乘以指定标量
    /// </summary>
    /// <param name="a">要乘的浮点数组</param>
    /// <param name="b">标量乘数</param>
    /// <returns>
    /// 乘积结果的新数组。如果a为null则返回null。
    /// 如果b约等于1，则直接返回原数组（避免不必要的复制）。
    /// </returns>
    public static float[]? MulFloats(float[]? a, float b)
    {
        if (a is null) return null;
        if (ApproximatelyEqual(b, 1f)) return a;
        var c = new float[a.Length];
        for (var i = 0; i < a.Length; i++) c[i] = a[i] * b;
        return c;
    }

    /// <summary>
    /// 将多个动画合并为单个动画数据
    /// </summary>
    /// <param name="animations">要合并的动画列表</param>
    /// <returns>
    /// 合并后的AnimationData实例，包含所有输入动画的数据。
    /// 新动画的IsLoop属性为所有输入动画IsLoop的逻辑或结果。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 合并规则：
    /// <list type="bullet">
    ///   <item><description>新动画数据 = 动画1 + 动画2 + 动画3 + ...</description></item>
    ///   <item><description>IsLoop = 动画1.IsLoop | 动画2.IsLoop | ...</description></item>
    ///   <item><description>IsMix由各Timeline的MixId决定</description></item>
    /// </list>
    /// </para>
    /// </remarks>
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
    /// <param name="animation">源动画</param>
    /// <param name="newAnimation">目标动画数据</param>
    /// <param name="maxFrame">最大帧数</param>
    private static void SetLoopDataByFrame(Animation animation, AnimationData newAnimation, int maxFrame)
    {
        if (!animation.IsLoop || animation.Timeline[^1].EndFrame == maxFrame) return;
        SetAttachmentsData(newAnimation, animation.Timeline[^1], animation.Timeline[^1].EndFrame, maxFrame);
    }

    /// <summary>
    /// 设置循环数据
    /// </summary>
    /// <param name="animation">源动画</param>
    /// <param name="newAnimation">目标动画数据</param>
    /// <param name="maxFrame">最大帧数</param>
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
    /// <param name="newAnimation">目标动画数据</param>
    /// <param name="timeline">时间线</param>
    /// <param name="startFrame">起始帧</param>
    /// <param name="endFrame">结束帧</param>
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
    /// <param name="newAnimation">动画数据</param>
    /// <param name="frame">帧索引</param>
    /// <returns>指定帧的Attachment实例</returns>
    private static Attachment GetAttachmentData(AnimationData newAnimation, int frame)
    {
        if (!newAnimation.Data.TryGetValue(frame, out var data))
            newAnimation.Data.Add(frame, data = new Attachment());
        return data;
    }

    /// <summary>
    /// 计算关键帧层的矩形区域
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <returns>包含层图像区域的SKRectI结构</returns>
    public static SKRectI CalculateRectangle(KeyframeLayer layer)
    {
        return SKRectI.Create((int)layer.MinAndMaxSrcPoints[0], (int)layer.MinAndMaxSrcPoints[1],
            (int)layer.Width, (int)layer.Height);
    }

    /// <summary>
    /// 判断两个浮点数是否近似相等
    /// </summary>
    /// <param name="a">第一个浮点数</param>
    /// <param name="b">第二个浮点数</param>
    /// <param name="epsilon">容差值，默认为0.000001</param>
    /// <returns>
    /// 如果两个数的差值小于容差则返回true，否则返回false。
    /// 如果任一参数为null，返回false。
    /// </returns>
    public static bool ApproximatelyEqual(float? a, float? b, float epsilon = 0.000001f)
    {
        if (a is null || b is null) return false;
        return Math.Abs((float)(a - b)) < epsilon;
    }
}
