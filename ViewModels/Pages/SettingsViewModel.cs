namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private string _logs=string.Empty;
}