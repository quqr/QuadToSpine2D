namespace QTSCore.Interfaces;

/// <summary>
///     转换结果输出接口，解耦核心层对 UI 层的结果通知依赖。
/// </summary>
public interface IConversionResult
{
    /// <summary>
    ///     结果 JSON 文件路径
    /// </summary>
    string ResultJsonUrl { get; set; }

    /// <summary>
    ///     结果链接是否可用
    /// </summary>
    bool ResultJsonUrlIsEnabled { get; set; }
}