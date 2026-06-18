using ObservableCollections;

namespace QTSAvalonia.ViewModels.Pages;

/// <summary>
/// 设置页面的视图模型
/// </summary>
/// <remarks>
/// <para>
/// SettingsViewModel类管理应用程序设置页面的状态和行为。
/// 主要功能是显示和管理日志队列。
/// </para>
/// <para>
/// 此类使用[SingletonService]特性标记，由依赖注入容器管理为单例服务。
/// </para>
/// </remarks>
[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    /// <summary>
    /// 获取或设置日志队列
    /// </summary>
    /// <value>
    /// 包含日志消息TextBlock的ObservableQueue，最大容量为150条
    /// </value>
    /// <remarks>
    /// 日志队列用于在UI上显示最近的日志记录。
    /// 当队列超过最大容量时，最旧的记录会被自动移除。
    /// </remarks>
    [ObservableProperty] 
    private ObservableQueue<TextBlock> _logs = new(150);
    
    /// <summary>
    /// 初始化SettingsViewModel实例
    /// </summary>
    public SettingsViewModel()
    {
    }
}
