using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class DataBase : UserControl
{
    public DataBase()
    {
        DataContext = Instances.ServiceProvider.GetRequiredService<DataBaseViewModel>();
        InitializeComponent();
    }
}