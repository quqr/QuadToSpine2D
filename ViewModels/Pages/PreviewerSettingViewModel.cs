namespace QTSAvalonia.ViewModels.Pages;

public partial class PreviewerSettingViewModel : ViewModelBase
{
    [ObservableProperty] private int _imageScale = 1;
    [ObservableProperty] private int _canvasScale = 1;
}