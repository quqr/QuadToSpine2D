using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QTSAvalonia.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private int counter;

    [RelayCommand]
    private void AddCounter()
    {
        Counter++;
    }
}