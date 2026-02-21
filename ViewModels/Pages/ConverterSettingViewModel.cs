namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class ConverterSettingViewModel : ViewModelBase
{
    [ObservableProperty] private string _imageSavePath = Directory.GetCurrentDirectory();
    [ObservableProperty] private bool _isLoopingAnimation;
    [ObservableProperty] private bool _isPrettyJsonPrint;
    [ObservableProperty] private string _resultSavePath = Directory.GetCurrentDirectory();
    [ObservableProperty] private int _scaleFactor = 1;

    public int FogTexId { get; set; } = 1000;
    public List<List<string?>> ImagePath { get; set; } = [];
    public float Fps { get; set; } = 1 / 60f;

    [RelayCommand]
    private async Task OpenJsonSavePath()
    {
        var folders = await AvaloniaFilePickerService.OpenFileSavePathAsync();
        if (folders is not null && folders.Count > 0)
            ResultSavePath = Uri.UnescapeDataString(folders[0].Path.AbsolutePath);
    }

    [RelayCommand]
    private async Task OpenImagesSavePath()
    {
        var folders = await AvaloniaFilePickerService.OpenFileSavePathAsync();
        if (folders is not null && folders.Count > 0)
            ImageSavePath = Uri.UnescapeDataString(folders[0].Path.AbsolutePath);
    }
}