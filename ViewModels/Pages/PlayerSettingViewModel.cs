namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class PlayerSettingViewModel : ViewModelBase
{
    [ObservableProperty] private int _canvasSize = 4096;
    [ObservableProperty] private float _fps = 60f;
    [ObservableProperty] private int _imageScaleFactor = 1;
}