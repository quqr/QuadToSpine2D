using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class ConverterSetting : UserControl
{
    public ConverterSetting()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<ConverterSettingViewModel>();
        InitializeComponent();
    }
}