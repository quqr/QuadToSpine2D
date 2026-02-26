using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class Player : UserControl
{
    public Player()
    {
        DataContext = Instances.ServiceProvider.GetService<PlayerViewModel>();
        InitializeComponent();
    }    
}