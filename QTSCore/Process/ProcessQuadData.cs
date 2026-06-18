using QTSAvalonia.Helper;
using QTSCore.Data.Quad;
using QTSCore.Interfaces;

namespace QTSCore.Process;

/// <summary>
/// 处理Quad JSON数据的核心类
/// </summary>
/// <remarks>
/// <para>
/// ProcessQuadData类是Quad到Spine2D转换流程的入口点。
/// 它协调了JSON文件加载、数据处理和结果输出的完整流程。
/// </para>
/// <para>
/// 典型使用流程：
/// <code>
/// new ProcessQuadData()
///     .LoadQuadJson(quadFilePath, true)
///     .ProcessJson();
/// </code>
/// </para>
/// </remarks>
public class ProcessQuadData : IProcessQuadData
{
    /// <summary>
    /// 获取加载的Quad JSON数据
    /// </summary>
    /// <value>
    /// 加载后的QuadJsonData实例，如果尚未加载则为null
    /// </value>
    public QuadJsonData? QuadData { get; private set; }

    /// <summary>
    /// 加载Quad JSON文件并返回当前实例以支持链式调用
    /// </summary>
    /// <param name="quadPath">Quad JSON文件的完整路径</param>
    /// <param name="isPostProcess">
    /// 是否执行后处理（包括动画合并等操作）。
    /// 设置为true时会执行CombineAnimations操作。
    /// </param>
    /// <returns>当前ProcessQuadData实例，支持链式调用</returns>
    /// <exception cref="ArgumentException">
    /// 当quadPath指向的文件无效或无法解析时抛出
    /// </exception>
    public ProcessQuadData LoadQuadJson(string quadPath, bool isPostProcess = false)
    {
        QuadData = new ProcessQuadJsonFile().LoadQuadJson(quadPath, isPostProcess);
        return this;
    }

    /// <summary>
    /// 处理JSON数据并生成Spine格式输出
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 当QuadData为null时抛出，提示用户选择正确的Quad文件
    /// </exception>
    public void ProcessJson()
    {
        if (QuadData is null)
            throw new ArgumentException("Please select correct Quad file");

        var spineJson = new ProcessSpine2DJson(QuadData);
        var outputPath = spineJson.Process().WriteToJson();
        Instances.Converter.ResultJsonUrl = outputPath;
        Instances.Converter.ResultJsonUrlIsEnabled = true;
    }

    /// <summary>
    /// 显式接口实现：加载Quad JSON文件
    /// </summary>
    IProcessQuadData IProcessQuadData.LoadQuadJson(string quadPath, bool isPostProcess)
    {
        return LoadQuadJson(quadPath, isPostProcess);
    }
}
