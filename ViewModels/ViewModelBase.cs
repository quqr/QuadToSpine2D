using CommunityToolkit.Mvvm.ComponentModel;

namespace QTSAvalonia.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    protected ViewModelBase()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
    }
}