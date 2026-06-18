using QTSAvalonia.ViewModels.Pages;
using QTSCore.Data.Quad;

namespace QTSCore.Data;

/// <summary>
/// 表示图层的数据结构，用于连接Quad和Spine数据格式
/// </summary>
/// <remarks>
/// <para>
/// LayerData类是转换过程中的关键数据结构，它存储了单个图层的所有必要信息，
/// 包括纹理ID、图像名称、皮肤索引等属性。
/// </para>
/// <para>
/// 每个LayerData实例对应一个唯一的图层，用于生成Spine格式的输出。
/// </para>
/// </remarks>
public class LayerData
{
    private readonly string _texId;

    /// <summary>
    /// 获取关联的关键帧层数据
    /// </summary>
    public KeyframeLayer KeyframeLayer { get; init; }

    /// <summary>
    /// 获取插槽和图像的组合名称
    /// </summary>
    /// <remarks>
    /// 此名称用于在Spine中唯一标识此图层，格式通常为"Slice_{imageIndex}_{texId}_{skinIndex}_{copyIndex}"
    /// </remarks>
    public string SlotAndImageName { get; init; }

    /// <summary>
    /// 获取基础皮肤附件名称
    /// </summary>
    /// <remarks>
    /// 用于关联网格（LinkedMesh）的父网格名称
    /// </remarks>
    public string BaseSkinAttachmentName { get; init; }

    /// <summary>
    /// 获取或设置皮肤名称
    /// </summary>
    public string SkinName { get; set; }

    /// <summary>
    /// 获取图像索引
    /// </summary>
    public int ImageIndex { get; init; }

    /// <summary>
    /// 获取或设置皮肤索引
    /// </summary>
    public int SkinIndex { get; set; }

    /// <summary>
    /// 获取复制索引
    /// </summary>
    /// <remarks>
    /// 用于区分同一图层的多个副本
    /// </remarks>
    public int CopyIndex { get; init; }

    /// <summary>
    /// 获取混合模式ID
    /// </summary>
    /// <value>
    /// 从KeyframeLayer继承的混合模式标识符
    /// </value>
    public int BlendId => KeyframeLayer.BlendId;

    /// <summary>
    /// 获取纹理ID
    /// </summary>
    /// <remarks>
    /// <para>
    /// 纹理ID用于标识图层使用的纹理资源。
    /// 当ID等于FogTexId时，会自动转换为"Fog"字符串。
    /// </para>
    /// <para>
    /// 设置此属性时会自动计算BaseSkinAttachmentName。
    /// </para>
    /// </remarks>
    public string TexId
    {
        get => _texId;
        init
        {
            _texId = value.Equals(ConverterSettingViewModel.FogTexId.ToString()) ? "Fog" : value;
            BaseSkinAttachmentName = $"Slice_{ImageIndex}_{_texId}_0_{CopyIndex}";
        }
    }
}
