using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class Previewer : UserControl
{
    public Previewer()
    {
        DataContext = Instances.ServiceProvider.GetService<PreviewerViewModel>();
        InitializeComponent();
    }


}