namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class PreviewerSettingViewModel : ViewModelBase
{
    [ObservableProperty] private int _canvasSize = 3200;
    [ObservableProperty] private float _fps = 60f;
    [ObservableProperty] private int _imageScaleFactor = 4;
    
    
}