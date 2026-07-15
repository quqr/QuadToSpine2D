namespace QTSCore.Interfaces;

/// <summary>
/// 转换器配置接口，解耦核心层与 UI 层的配置依赖。
/// </summary>
/// <remarks>
/// 实现此接口的类提供转换过程中所需的图像路径、保存路径、缩放因子等配置。
/// </remarks>
public interface IConverterSettings
{
    /// <summary>
    /// 图像保存路径
    /// </summary>
    string ImageSavePath { get; }

    /// <summary>
    /// 结果保存路径
    /// </summary>
    string ResultSavePath { get; }

    /// <summary>
    /// 是否启用循环动画
    /// </summary>
    bool IsLoopingAnimation { get; }

    /// <summary>
    /// 缩放因子
    /// </summary>
    int ScaleFactor { get; }

    /// <summary>
    /// 雾纹理 ID
    /// </summary>
    int FogTexId { get; }

    /// <summary>
    /// 每秒帧数的时间间隔（秒）
    /// </summary>
    float Fps { get; }

    /// <summary>
    /// 图像路径列表（二维：外层为元素，内层为每个元素的图像路径）
    /// </summary>
    List<List<string?>> ImagePath { get; }
}
