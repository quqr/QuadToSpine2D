namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<TextBlock> _logs = [];
}