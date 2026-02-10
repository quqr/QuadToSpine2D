using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.UserControls;
using QTSCore.Data;
using QTSCore.Process;

namespace QTSAvalonia.ViewModels.Pages;
[SingletonService]
public partial class ConverterViewModel : ViewModelBase
{
    
    [ObservableProperty] private ObservableCollection<ElementViewModel> _elements = [];

    [ObservableProperty] private float _progress;
    [ObservableProperty] private string _quadFileName = "Random Quad File";

    private string _quadFilePath = string.Empty;

    [ObservableProperty] private string _resultJsonUrl = "Result json path";

    public ConverterViewModel()
    {
        Elements.CollectionChanged += UpdateElementsIndex;
    }

    private void UpdateElementsIndex(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (var index = 0; index < Elements.Count; index++) 
        {
            Elements[index].Index = index;
        }
    }

    private List<List<string?>> ProcessImagePaths()
    {
        //var imagePaths = _elements.Select(element => element.ImagePaths).ToList();

        List<List<string?>> imagePaths =
        [
            [
                @"F:\Codes\Test\ps4 odin HD_Gwendlyn.0.gnf.png"
            ],
            [
                @"F:\Codes\Test\ps4 odin HD_Gwendlyn.1.gnf.png"
            ],
            [@"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png"]
        ];

        var maxCount = imagePaths.Max(paths => paths.Count);

        return imagePaths.Select(paths =>
                             Enumerable.Range(0, maxCount)
                                       .Select(index => index < paths.Count ? paths[index] : null)
                                       .ToList())
                         .ToList();
    }

    [RelayCommand]
    private async Task OpenQuadFilePicker()
    {
        LoggerHelper.Info("Opening quad file picker in ConverterModelView");
        var file = await AvaloniaFilePickerService.OpenQuadFileAsync();
        if (file is not null && file.Count > 0)
        {
            QuadFileName  = file[0].Name;
            _quadFilePath = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
            LoggerHelper.Info($"Selected quad file: {QuadFileName}");
        }
        else
        {
            LoggerHelper.Warn("No quad file selected in ConverterModelView");
        }
    }

    [RelayCommand]
    private void ProcessData()
    {
        LoggerHelper.Info("Starting data processing");
        Task.Run(() =>
        {
            try
            {
                var imagePaths = ProcessImagePaths();
                LoggerHelper.Debug($"Processing {imagePaths.Count} image paths");
                Instances.ConverterSetting.ImagePath = imagePaths;
                new ProcessQuadData()
                    .LoadQuadJson(_quadFilePath)
                    .ProcessJson();
                
                LoggerHelper.Info("Data processing completed successfully");
                Console.WriteLine("Process Complete!");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error during data processing", ex);
            }
        });
    }

    [RelayCommand]
    private void AddNewElement()
    {
        LoggerHelper.Debug($"Adding new element. Current count: {Elements.Count}");
        Elements.Add(new ElementViewModel(vm => Elements.RemoveAt(vm.Index)));
        LoggerHelper.Debug($"Element added. New count: {Elements.Count}");
    }

}