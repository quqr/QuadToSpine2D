namespace QTSAvalonia.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private int _counter;

    [RelayCommand]
    private void AddCounter()
    {
        Counter++;
    }
}