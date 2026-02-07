namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class ConverterSettingViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isLoopingAnimation;
    [ObservableProperty] private bool _isPrettyJsonPrint;

    [ObservableProperty] private string _scaleFactorStr;

    private float ScaleFactor => int.TryParse(_scaleFactorStr, out var res) ? res : 1;
}