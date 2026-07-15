using QTSCore.Data.Quad;
using SkiaSharp;

namespace QTSCore.Interfaces;

/// <summary>
/// Quad 渲染器接口，解耦 UI 层对具体渲染实现的依赖。
/// </summary>
public interface IQuadRenderer
{
    /// <summary>
    /// 当前渲染时间
    /// </summary>
    float CurrentTime { get; set; }

    /// <summary>
    /// 清空画布
    /// </summary>
    void ClearCanvas();

    /// <summary>
    /// 绘制骨骼
    /// </summary>
    void DrawSkeletonBones();

    /// <summary>
    /// 绘制附件
    /// </summary>
    void DrawAttach(int skeletonIndex, int timelineIndex);

    /// <summary>
    /// 获取画布快照
    /// </summary>
    SKBitmap Snapshot();

    /// <summary>
    /// 添加源图像
    /// </summary>
    void AddSourceImage(int index, SKBitmap bitmap);

    /// <summary>
    /// 重置渲染器状态
    /// </summary>
    void Reset();
}
