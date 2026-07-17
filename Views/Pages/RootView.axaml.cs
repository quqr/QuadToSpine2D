using System.ComponentModel;
using System.Reflection;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QTSAvalonia.ViewModels.Pages;
using SukiUI.Controls;

namespace QTSAvalonia.Views.Pages;

public partial class RootView : SukiWindow
{
    private PropertyInfo? _selectedIndexProperty;
    private SukiSideMenu? _sideMenu;

    public RootView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _sideMenu = this.FindControl<SukiSideMenu>("SideMenu");
        if (_sideMenu != null)
            // 用反射查找 SelectedIndex 属性（SukiSideMenu 可能通过基类提供）
            _selectedIndexProperty = _sideMenu.GetType().GetProperty("SelectedIndex");

        if (DataContext is RootViewModel vm) vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RootViewModel.SelectedPageIndex)) return;
        if (_sideMenu == null || sender is not RootViewModel vm) return;

        Dispatcher.UIThread.Post(() =>
        {
            // 尝试通过反射设置 SelectedIndex
            if (_selectedIndexProperty != null)
            {
                _selectedIndexProperty.SetValue(_sideMenu, vm.SelectedPageIndex);
                return;
            }

            // 回退方案：找到对应索引的 SukiSideMenuItem 并模拟点击
            var items = _sideMenu.Items?.Cast<object>().ToList();
            if (items == null || vm.SelectedPageIndex < 0 || vm.SelectedPageIndex >= items.Count)
                return;

            if (items[vm.SelectedPageIndex] is SukiSideMenuItem targetItem)
            {
                // 尝试调用 IsSelected 或 Click
                var isSelectedProp = typeof(SukiSideMenuItem).GetProperty("IsSelected");
                if (isSelectedProp != null)
                {
                    // 先取消其他项的选中
                    foreach (var item in items.OfType<SukiSideMenuItem>())
                        isSelectedProp.SetValue(item, false);
                    isSelectedProp.SetValue(targetItem, true);
                }
            }
        });
    }
}