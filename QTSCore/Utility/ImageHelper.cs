using QTSCore.Data.Quad;
using SkiaSharp;

namespace QTSCore.Utility;

/// <summary>
///     图像处理辅助工具，提供关键帧层的矩形区域计算。
/// </summary>
public static class ImageHelper
{
    /// <summary>
    ///     计算关键帧层的矩形区域
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <returns>包含层图像区域的 SKRectI 结构</returns>
    public static SKRectI CalculateRectangle(KeyframeLayer layer)
    {
        return SKRectI.Create((int)layer.MinAndMaxSrcPoints[0], (int)layer.MinAndMaxSrcPoints[1],
            (int)layer.Width, (int)layer.Height);
    }
}