namespace VanillawareConverter.Ftex;

/// <summary>
/// 表示Vanillaware游戏支持的平台类型
/// </summary>
public enum GamePlatform
{
    /// <summary>
    /// 未知平台
    /// </summary>
    Unknown,
    
    /// <summary>
    /// PlayStation 2平台
    /// </summary>
    PS2,
    
    /// <summary>
    /// PlayStation 3平台
    /// </summary>
    PS3,
    
    /// <summary>
    /// PlayStation 4平台
    /// </summary>
    PS4,
    
    /// <summary>
    /// PlayStation Portable平台
    /// </summary>
    PSP,
    
    /// <summary>
    /// PlayStation Vita平台
    /// </summary>
    PSVita,
    
    /// <summary>
    /// Nintendo DS平台
    /// </summary>
    NDS,
    
    /// <summary>
    /// Nintendo Wii平台
    /// </summary>
    Wii,
    
    /// <summary>
    /// Nintendo Switch平台
    /// </summary>
    Switch
}

/// <summary>
/// 表示纹理的格式类型
/// </summary>
public enum TextureFormatType
{
    /// <summary>
    /// 未知格式
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 32位RGBA格式
    /// </summary>
    RGBA32,
    
    /// <summary>
    /// 32位BGRA格式
    /// </summary>
    BGRA32,
    
    /// <summary>
    /// 32位ARGB格式
    /// </summary>
    ARGB32,
    
    /// <summary>
    /// 4位索引颜色格式
    /// </summary>
    Indexed4,
    
    /// <summary>
    /// 8位索引颜色格式
    /// </summary>
    Indexed8,
    
    /// <summary>
    /// DXT1压缩格式
    /// </summary>
    DXT1,
    
    /// <summary>
    /// DXT3压缩格式
    /// </summary>
    DXT3,
    
    /// <summary>
    /// DXT5压缩格式
    /// </summary>
    DXT5,
    
    /// <summary>
    /// BC3压缩格式
    /// </summary>
    BC3,
    
    /// <summary>
    /// BC4压缩格式
    /// </summary>
    BC4,
    
    /// <summary>
    /// BC7压缩格式
    /// </summary>
    BC7,
    
    /// <summary>
    /// CMPR压缩格式(GameCube/Wii)
    /// </summary>
    CMPR,
    
    /// <summary>
    /// C4格式(GameCube/Wii 4位索引)
    /// </summary>
    C4,
    
    /// <summary>
    /// C8格式(GameCube/Wii 8位索引)
    /// </summary>
    C8,
    
    /// <summary>
    /// C14X2格式(GameCube/Wii 14位索引)
    /// </summary>
    C14X2
}

/// <summary>
/// 表示纹理的信息
/// </summary>
public class TextureInfo
{
    /// <summary>
    /// 纹理宽度（像素）
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 纹理高度（像素）
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 纹理格式类型
    /// </summary>
    public TextureFormatType Format { get; set; }
    
    /// <summary>
    /// 是否经过扭曲处理
    /// </summary>
    public bool IsSwizzled { get; set; }
    
    /// <summary>
    /// Mipmap层级数量
    /// </summary>
    public int MipCount { get; set; }
    
    /// <summary>
    /// 调色板数据（用于索引颜色格式）
    /// </summary>
    public byte[]? Palette { get; set; }
    
    /// <summary>
    /// 调色板颜色数量
    /// </summary>
    public int PaletteColorCount { get; set; }
}

/// <summary>
/// FTEX文件解析器接口
/// </summary>
/// <remarks>
/// 实现此接口以支持不同平台的FTEX纹理文件解析
/// </remarks>
public interface IFtexParser
{
    /// <summary>
    /// 获取解析器支持的游戏平台
    /// </summary>
    GamePlatform Platform { get; }
    
    /// <summary>
    /// 检查是否可以解析指定的文件数据
    /// </summary>
    /// <param name="fileData">文件字节数据</param>
    /// <returns>如果可以解析则返回true，否则返回false</returns>
    bool CanParse(byte[] fileData);
    
    /// <summary>
    /// 解析FTEX文件并提取纹理数据
    /// </summary>
    /// <param name="fileData">文件字节数据</param>
    /// <param name="outputPrefix">输出文件名前缀</param>
    /// <returns>解析后的图像结果列表</returns>
    List<ImageResult> Parse(byte[] fileData, string outputPrefix);
}

/// <summary>
/// 扭曲算法接口
/// </summary>
/// <remarks>
/// 用于处理不同平台的纹理数据扭曲/解扭曲操作
/// </remarks>
public interface ISwizzleAlgorithm
{
    /// <summary>
    /// 解扭曲纹理数据
    /// </summary>
    /// <param name="data">扭曲的纹理数据</param>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="bytesPerPixel">每像素字节数</param>
    /// <returns>解扭曲后的纹理数据</returns>
    byte[] Unswizzle(byte[] data, int width, int height, int bytesPerPixel);
    
    /// <summary>
    /// 扭曲纹理数据
    /// </summary>
    /// <param name="data">原始纹理数据</param>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="bytesPerPixel">每像素字节数</param>
    /// <returns>扭曲后的纹理数据</returns>
    byte[] Swizzle(byte[] data, int width, int height, int bytesPerPixel);
}
