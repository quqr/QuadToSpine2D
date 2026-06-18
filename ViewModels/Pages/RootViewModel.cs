namespace QTSAvalonia.ViewModels.Pages;

/// <summary>
/// 根视图模型，管理主页面导航
/// </summary>
/// <remarks>
/// <para>
/// RootViewModel类是应用程序主界面的根视图模型，
/// 负责管理当前显示的页面类型。
/// </para>
/// <para>
/// 此类使用[SingletonService]特性标记，由依赖注入容器管理为单例服务。
/// </para>
/// </remarks>
[SingletonService]
public partial class RootViewModel : ViewModelBase
{
    /// <summary>
    /// 获取或设置当前页面类型
    /// </summary>
    /// <value>
    /// 当前显示的页面设置实例
    /// </value>
    [ObservableProperty] 
    private Settings _pageType = new();
}
