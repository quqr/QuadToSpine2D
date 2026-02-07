using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class Settings : UserControl
{
    public Settings()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }
}