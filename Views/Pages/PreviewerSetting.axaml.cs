using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class PreviewerSetting : UserControl
{
    public PreviewerSetting()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<PreviewerSettingViewModel>();
        InitializeComponent();
    }
}