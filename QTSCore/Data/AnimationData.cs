using QTSCore.Data.Quad;

namespace QTSCore.Data;

/// <summary>
/// 表示动画数据的类，存储动画的循环、混合属性以及帧到附件的映射
/// </summary>
/// <remarks>
/// <para>
/// AnimationData类用于存储转换过程中的动画信息。
/// 它使用字典结构按帧索引存储附件数据，支持高效的帧查找操作。
/// </para>
/// <para>
/// 此类是Quad到Spine转换过程中的核心数据结构之一。
/// </para>
/// </remarks>
public class AnimationData
{
    /// <summary>
    /// 获取或设置动画是否循环播放
    /// </summary>
    /// <value>
    /// 如果动画应循环播放则为true，否则为false
    /// </value>
    public bool IsLoop { get; set; }

    /// <summary>
    /// 获取或设置动画是否包含混合效果
    /// </summary>
    /// <value>
    /// 如果动画包含混合效果则为true，否则为false
    /// </value>
    public bool IsMix { get; set; }

    /// <summary>
    /// 获取帧到附件的映射字典
    /// </summary>
    /// <value>
    /// 键为帧索引，值为该帧对应的Attachment实例
    /// </value>
    /// <remarks>
    /// 此字典存储每个帧点上需要显示或隐藏的附件信息。
    /// 通过帧索引可以快速查找对应的附件数据。
    /// </remarks>
    public Dictionary<int, Attachment> Data { get; set; } = [];
}

/// <summary>
/// 表示单个帧上的附件集合
/// </summary>
/// <remarks>
/// <para>
/// Attachment类管理两个附件列表：
/// <list type="bullet">
///   <item><description>DisplayAttachments: 在此帧需要显示的附件</description></item>
///   <item><description>ConcealAttachments: 在此帧需要隐藏的附件</description></item>
/// </list>
/// </para>
/// </remarks>
public class Attachment
{
    /// <summary>
    /// 获取需要显示的附件时间线列表
    /// </summary>
    /// <value>
    /// 包含所有需要在此帧显示的Timeline实例的列表
    /// </value>
    public List<Timeline> DisplayAttachments { get; } = [];

    /// <summary>
    /// 获取需要隐藏的附件时间线列表
    /// </summary>
    /// <value>
    /// 包含所有需要在此帧隐藏的Timeline实例的列表
    /// </value>
    public List<Timeline> ConcealAttachments { get; } = [];
}
