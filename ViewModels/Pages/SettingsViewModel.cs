namespace QTSAvalonia.ViewModels.Pages;
[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private int _counter;

    [RelayCommand]
    private void AddCounter()
    {
        Counter++;
    }
}