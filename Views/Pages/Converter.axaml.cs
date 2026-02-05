using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Views.Pages;

public partial class Converter : UserControl
{
    public Converter()
    {
        DataContext = new ConverterViewModel();
        InitializeComponent();
    }
}