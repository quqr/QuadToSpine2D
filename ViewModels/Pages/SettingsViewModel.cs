using ObservableCollections;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableQueue<TextBlock> _logs= new (150);
    
    public SettingsViewModel()
    {
    }
}