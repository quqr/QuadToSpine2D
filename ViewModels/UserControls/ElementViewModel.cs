using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QTSAvalonia.Utilities;

namespace QTSAvalonia.ViewModels.UserControls;

public partial class ElementViewModel : ViewModelBase
{
    private readonly Action<ElementViewModel>? _onDeleteRequested;

    [ObservableProperty] private ObservableCollection<HyperLinkTrimButtonViewModel> _imagePathHyperlinkButtons = [];
    [ObservableProperty] private List<string> _imagePaths = [];

    [ObservableProperty] private int _index;

    public ElementViewModel(Action<ElementViewModel>? onDeleteRequested)
    {
        _onDeleteRequested = onDeleteRequested;
    }

    public ElementViewModel()
    {
    }

    [RelayCommand]
    private void DeleteElement()
    {
        _onDeleteRequested(this);
    }

    [RelayCommand]
    private async Task AddImagePaths()
    {
        var files = await InstanceSingleton.Instance.FilePickerService.OpenImageFilesAsync();
        if (files != null)
            foreach (var file in files)
            {
                ImagePaths.Add(Uri.UnescapeDataString(file.Path.AbsolutePath));
                ImagePathHyperlinkButtons.Add(new HyperLinkTrimButtonViewModel
                {
                    ImagePath = file.Path.AbsolutePath
                });
            }
    }
}