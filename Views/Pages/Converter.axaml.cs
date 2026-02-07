using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class Converter : UserControl
{
    public Converter()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<ConverterViewModel>();
        InitializeComponent();
    }
}