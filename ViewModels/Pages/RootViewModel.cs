namespace QTSAvalonia.ViewModels.Pages;

/// <summary>
/// 根视图模型，管理主页面导航
/// </summary>
/// <remarks>
/// 管理应用程序主界面的页面切换。
/// 使用[SingletonService]特性标记为单例服务。
/// </remarks>
[SingletonService]
public partial class RootViewModel : ViewModelBase
{
    /// <summary>
    /// 当前选中的侧边栏菜单索引（0=Home/Converter, 1=File Manager, 2=Previewer, 3=Setting）
    /// </summary>
    [ObservableProperty]
    private int _selectedPageIndex;

    /// <summary>
    /// 导航到指定页面索引
    /// </summary>
    /// <param name="pageIndex">目标页面索引</param>
    public void NavigateTo(int pageIndex)
    {
        SelectedPageIndex = pageIndex;
        LoggerHelper.Info($"[Navigation] Navigated to page {pageIndex}");
    }
}
