using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QTSAvalonia.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    int counter;
    
    [RelayCommand]
    void AddCounter()
    {
        Counter++;
    }
}