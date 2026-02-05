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
        LoggerHelper.Debug($"ElementViewModel created with delete handler: {_onDeleteRequested != null}");
    }

    public ElementViewModel()
    {
        LoggerHelper.Debug("ElementViewModel created without delete handler");
    }

    protected override void Initialize()
    {
        LoggerHelper.Info("ElementViewModel initialized");
        base.Initialize();
    }

    [RelayCommand]
    private void DeleteElement()
    {
        LoggerHelper.Info($"Deleting element at index {_index}");
        _onDeleteRequested(this);
    }

    [RelayCommand]
    private async Task AddImagePaths()
    {
        LoggerHelper.Info("Adding image paths");
        var files = await AvaloniaFilePickerService.OpenImageFilesAsync();
        if (files != null)
        {
            LoggerHelper.Debug($"Selected {files.Count} image files");
            foreach (var file in files)
            {
                var imagePath = Uri.UnescapeDataString(file.Path.AbsolutePath);
                ImagePaths.Add(imagePath);
                ImagePathHyperlinkButtons.Add(new HyperLinkTrimButtonViewModel
                {
                    ImagePath = file.Path.AbsolutePath
                });
                LoggerHelper.Debug($"Added image: {imagePath}");
            }
        }
        else
        {
            LoggerHelper.Warn("No files selected");
        }
    }
}