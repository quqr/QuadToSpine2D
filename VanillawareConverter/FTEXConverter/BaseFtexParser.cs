namespace VanillawareConverter.Ftex.Parsers;

/// <summary>
///     FTEX 平台解析器的抽象基类，实现策略模式（strategy pattern）。
///     子类通过重写 <see cref="MinimumFileLength" />、<see cref="CheckMagic" />
///     与 <see cref="ParseCore" /> 三个钩子提供平台特定行为，公共的空检查、
///     长度检查、结果列表装配等流程统一由本基类负责。
/// </summary>
/// <remarks>
///     本类不持有任何平台特定状态；多平台共用逻辑应放在此处，
///     仅属于单一平台的解码细节应保留在对应子类中。
/// </remarks>
public abstract class BaseFtexParser : IFtexParser
{
    /// <summary>
    ///     尝试解析该平台格式所需的最小文件长度。
    /// </summary>
    protected abstract int MinimumFileLength { get; }

    /// <inheritdoc />
    public abstract GamePlatform Platform { get; }

    /// <inheritdoc />
    public bool CanParse(byte[]? fileData)
    {
        if (fileData == null || fileData.Length < MinimumFileLength)
            return false;

        return CheckMagic(fileData);
    }

    /// <inheritdoc />
    public List<ImageResult> Parse(byte[] fileData, string outputPrefix)
    {
        var results = new List<ImageResult>();

        if (!CanParse(fileData))
            return results;

        ParseCore(fileData, outputPrefix, results);

        return results;
    }

    /// <summary>
    ///     在通过 <see cref="MinimumFileLength" /> 检查后，验证文件魔数是否匹配当前平台。
    /// </summary>
    /// <param name="fileData">已保证非 null 且长度不小于 <see cref="MinimumFileLength" />。</param>
    /// <returns>匹配则返回 true，否则返回 false。</returns>
    protected abstract bool CheckMagic(byte[] fileData);

    /// <summary>
    ///     平台特定的解析逻辑。每发现一张纹理应向 <paramref name="results" /> 追加一个
    ///     <see cref="ImageResult" />。
    /// </summary>
    /// <param name="fileData">已保证通过 <see cref="CanParse" /> 校验的文件数据。</param>
    /// <param name="outputPrefix">输出文件名前缀。</param>
    /// <param name="results">待填充的图像结果列表。</param>
    protected abstract void ParseCore(byte[] fileData, string outputPrefix, List<ImageResult> results);
}