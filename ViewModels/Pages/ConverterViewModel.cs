using System.Collections.Specialized;
using QTSAvalonia.ViewModels.UserControls;
using QTSCore.Process;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class ConverterViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ElementViewModel> _elements = [];
    
    [ObservableProperty]
    private string _quadFileName = string.Empty;

    private string _quadFilePath = string.Empty;

    [ObservableProperty]
    private string _resultJsonUrl = "Result json path";

    [ObservableProperty]
    private bool _resultJsonUrlIsEnabled;

    [ObservableProperty]
    private bool _isProcessing;

    public ConverterViewModel()
    {
        Elements.CollectionChanged += OnElementsCollectionChanged;
    }

    private void OnElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 更新所有元素的索引
        for (var i = 0; i < Elements.Count; i++)
        {
            Elements[i].Index = i;
        }
    }

    private List<List<string?>>? ProcessImagePaths()
    {
        if (Elements.Count == 0)
        {
            LoggerHelper.Warning("No elements found for processing");
            ToastHelper.Error("Please add at least one element with images");
            return null;
        }

        // 提取所有非空图片路径
        var validElements = Elements
            .Where(e => e.ImagePaths.Count > 0)
            .ToList();

        if (validElements.Count == 0)
        {
            LoggerHelper.Warning("No valid image paths found");
            ToastHelper.Error("No valid image paths found in elements");
            return null;
        }

        var maxImages = validElements.Max(e => e.ImagePaths.Count);
        var result = new List<List<string?>>(validElements.Count);

        foreach (var element in validElements)
        {
            var paths = new List<string?>(maxImages);
            for (var i = 0; i < maxImages; i++)
            {
                paths.Add(i < element.ImagePaths.Count ? element.ImagePaths[i] : null);
            }
            result.Add(paths);
        }

        return result;
    }

    [RelayCommand]
    private async Task OpenQuadFilePickerAsync()
    {
        LoggerHelper.Info("Opening quad file picker");
        
        var files = await AvaloniaFilePickerService.OpenQuadFileAsync();
        if (files?.Any() != true) 
        {
            LoggerHelper.Warning("No quad file selected");
            return;
        }

        var selectedFile = files[0];
        QuadFileName = selectedFile.Name;
        _quadFilePath = selectedFile.Path.LocalPath;
        
        LoggerHelper.Info($"Selected quad file: {QuadFileName}");
    }

    [RelayCommand]
    private async Task ProcessDataAsync()
    {
        LoggerHelper.Info("Starting data processing");
        IsProcessing = true;
        
        ResultJsonUrlIsEnabled = false;

        try
        {
            var imagePaths = ProcessImagePaths();
            if (imagePaths == null || imagePaths.Count == 0)
            {
                LoggerHelper.Error("Invalid image paths configuration");
                ToastHelper.Error("Invalid image configuration");
                return;
            }

            LoggerHelper.Debug($"Processing {imagePaths.Count} image groups");
            
            // 配置转换器
            Instances.ConverterSetting.ImagePath = imagePaths;
            
            // 执行处理流程
            await Task.Run(() => 
            {
                new ProcessQuadData()
                    .LoadQuadJson(_quadFilePath, true)
                    .ProcessJson();
            });

            LoggerHelper.Info("Data processing completed successfully");
            ToastHelper.Success("Processing completed successfully");
            ResultJsonUrlIsEnabled = true;
          
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Processing failed: {ex.Message}\n{ex.StackTrace}");
            ToastHelper.Error($"Processing failed: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void AddNewElement()
    {
        LoggerHelper.Debug($"Adding new element. Current count: {Elements.Count}");
        
        var newElement = new ElementViewModel(
             vm => Elements.RemoveAt(vm.Index)
        );
        
        Elements.Add(newElement);
        LoggerHelper.Debug($"Element added. New count: {Elements.Count}");
    }
}