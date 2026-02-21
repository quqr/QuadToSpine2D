namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class RootViewModel : ViewModelBase
{
    [ObservableProperty] private Settings _pageType = new();
}