using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class PlayerSetting: UserControl
{
    public PlayerSetting()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<PlayerSettingViewModel>();
        InitializeComponent();
    }
}