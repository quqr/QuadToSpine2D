using CommunityToolkit.Mvvm.ComponentModel;
using QTSAvalonia.Views.Pages;

namespace QTSAvalonia.ViewModels.Pages;

public partial class RootModelView : ViewModelBase
{
    [ObservableProperty] private Settings _pageType = new();
}