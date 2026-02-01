using CommunityToolkit.Mvvm.ComponentModel;

namespace QTSAvalonia.ViewModels.Pages;

public partial class ConverterModelView: ViewModelBase
{
    [ObservableProperty]
    private string quadFileName = "Random Quad File";
}