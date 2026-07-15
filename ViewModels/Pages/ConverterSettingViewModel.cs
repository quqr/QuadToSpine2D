using QTSCore.Interfaces;

namespace QTSAvalonia.ViewModels.Pages;

/// <summary>
/// 转换器设置页面的视图模型
/// </summary>
/// <remarks>
/// <para>
/// ConverterSettingViewModel类管理转换器的配置选项，
/// 包括图像保存路径、结果保存路径、动画设置等。
/// </para>
/// <para>
/// 此类使用[SingletonService]特性标记，由依赖注入容器管理为单例服务。
/// </para>
/// </remarks>
[SingletonService]
public partial class ConverterSettingViewModel : ViewModelBase, IConverterSettings
{
    /// <summary>
    /// 获取或设置图像保存路径
    /// </summary>
    /// <value>
    /// 图像文件的保存目录路径，默认为当前工作目录
    /// </value>
    [ObservableProperty] 
    private string _imageSavePath = Directory.GetCurrentDirectory();

    /// <summary>
    /// 获取或设置是否启用循环动画
    /// </summary>
    [ObservableProperty] 
    private bool _isLoopingAnimation;

    /// <summary>
    /// 获取或设置是否启用JSON美化打印
    /// </summary>
    [ObservableProperty] 
    private bool _isPrettyJsonPrint;

    /// <summary>
    /// 获取或设置结果保存路径
    /// </summary>
    /// <value>
    /// 结果JSON文件的保存目录路径，默认为当前工作目录
    /// </value>
    [ObservableProperty] 
    private string _resultSavePath = Directory.GetCurrentDirectory();

    /// <summary>
    /// 获取或设置缩放因子
    /// </summary>
    [ObservableProperty] 
    private int _scaleFactor = 1;

    /// <summary>
    /// 获取雾纹理ID
    /// </summary>
    /// <value>
    /// 用于标识雾效果纹理的特殊ID值
    /// </value>
    public int FogTexId => 1000;

    /// <summary>
    /// 获取或设置图像路径列表
    /// </summary>
    /// <value>
    /// 二维列表，外层表示元素，内层表示每个元素的图像路径
    /// </value>
    public List<List<string?>> ImagePath { get; set; } = [];

    /// <summary>
    /// 获取FPS（每秒帧数）的时间间隔
    /// </summary>
    /// <value>
    /// 每帧的时间间隔（秒），基于60FPS计算
    /// </value>
    public float Fps => 1 / 60f;

    /// <summary>
    /// 打开JSON保存路径选择器命令
    /// </summary>
    [RelayCommand]
    private async Task OpenJsonSavePath()
    {
        var folders = await AvaloniaFilePickerService.OpenFileSavePathAsync();
        if (folders is not null && folders.Count > 0)
            ResultSavePath = folders[0].Path.LocalPath;
    }

    /// <summary>
    /// 打开图像保存路径选择器命令
    /// </summary>
    [RelayCommand]
    private async Task OpenImagesSavePath()
    {
        var folders = await AvaloniaFilePickerService.OpenFileSavePathAsync();
        if (folders is not null && folders.Count > 0)
            ImageSavePath = folders[0].Path.LocalPath;
    }
}
