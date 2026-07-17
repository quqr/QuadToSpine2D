namespace VanillawareConverter.Mbs.Models;

/// <summary>
///     表示Vanillaware游戏平台标签
/// </summary>
/// <remarks>
///     用于标识不同游戏和平台的MBS文件格式
/// </remarks>
public enum PlatformTag
{
    /// <summary>
    ///     PlayStation 2 - Grim Grimoire
    /// </summary>
    Ps2Grim,

    /// <summary>
    ///     PlayStation 2 - Odin Sphere
    /// </summary>
    Ps2Odin,

    /// <summary>
    ///     Nintendo DS - Kumatanchi
    /// </summary>
    NdsKuma,

    /// <summary>
    ///     Nintendo Wii - Muramasa: The Demon Blade
    /// </summary>
    WiiMura,

    /// <summary>
    ///     PlayStation 3 - Dragon's Crown
    /// </summary>
    Ps3Drag,

    /// <summary>
    ///     PlayStation 3 - Odin Sphere Leifthrasir
    /// </summary>
    Ps3Odin,

    /// <summary>
    ///     PlayStation 4 - Odin Sphere Leifthrasir
    /// </summary>
    Ps4Odin,

    /// <summary>
    ///     PlayStation 4 - Dragon's Crown Pro
    /// </summary>
    Ps4Drag,

    /// <summary>
    ///     PlayStation 4 - 13 Sentinels: Aegis Rim
    /// </summary>
    Ps4Sent,

    /// <summary>
    ///     Nintendo Switch - 13 Sentinels: Aegis Rim
    /// </summary>
    SwiSent,

    /// <summary>
    ///     Nintendo Switch - Grim Grimoire HD
    /// </summary>
    SwiGrim,

    /// <summary>
    ///     Nintendo Switch - Unicorn Overlord
    /// </summary>
    SwiUnic,

    /// <summary>
    ///     PlayStation 4 - Unicorn Overlord
    /// </summary>
    Ps4Unic,

    /// <summary>
    ///     未知平台
    /// </summary>
    Unknown
}

/// <summary>
///     表示MBS文件中的区段信息
/// </summary>
public class SectionInfo
{
    /// <summary>
    ///     初始化区段信息的新实例
    /// </summary>
    public SectionInfo()
    {
    }

    /// <summary>
    ///     使用指定参数初始化区段信息的新实例
    /// </summary>
    /// <param name="p">区段数据偏移量在文件头中的位置</param>
    /// <param name="k">每个条目的大小</param>
    /// <param name="c">区段计数信息（偏移量和大小）</param>
    public SectionInfo(int p, int k, int[] c)
    {
        P = p;
        K = k;
        C = c;
    }

    /// <summary>
    ///     获取或设置区段数据偏移量在文件头中的位置
    /// </summary>
    public int P { get; set; }

    /// <summary>
    ///     获取或设置每个条目的大小（字节）
    /// </summary>
    public int K { get; set; }

    /// <summary>
    ///     获取或设置区段计数信息
    /// </summary>
    /// <remarks>
    ///     数组格式：[偏移量位置, 读取字节数]
    /// </remarks>
    public int[] C { get; set; } = [];
}

/// <summary>
///     表示平台特定的配置数据
/// </summary>
public class PlatformData
{
    /// <summary>
    ///     获取或设置平台标识标签
    /// </summary>
    /// <example>
    ///     例如："ps2 grim grimoire", "switch unicorn overlord"
    /// </example>
    public string IdTag { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置是否使用大端字节序
    /// </summary>
    public bool BigEndian { get; set; }

    /// <summary>
    ///     获取或设置区段信息列表
    /// </summary>
    public List<SectionInfo> Sections { get; set; } = [];
}