using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QTSAvalonia.Utilities;
using QTSAvalonia.ViewModels.UserControls;
using QTSCore.Data;
using QTSCore.Process;

namespace QTSAvalonia.ViewModels.Pages;

public partial class ConverterModelView : ViewModelBase
{
    [ObservableProperty]
    private string _quadFileName = "Random Quad File";

    [ObservableProperty]
    private ObservableCollection<ElementViewModel> _elements = [];
    
    [ObservableProperty] private float _progress = 56;
    
    [ObservableProperty] private string _resultJsonUrl = "Result json path";

    public ConverterModelView()
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

        return  imagePaths.Select(paths =>
                                   Enumerable.Range(0, maxCount)
                                             .Select(index => index < paths.Count ? paths[index] : null)
                                             .ToList())
                               .ToList();
    }
    [RelayCommand]
    private async Task OpenQuadFilePicker()
    {
        var file = await InstanceSingleton.Instance.FilePickerService.OpenQuadFileAsync();
        if (file is not null)
        {
            QuadFileName  = file[0].Name;
            _quadFilePath = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
        }
    }

    private string _quadFilePath = string.Empty;
    [RelayCommand]
    private void ProcessData()
    {
        Task.Run(() =>
        {
            var imagePaths = ProcessImagePaths();
            GlobalData.ImagePath = imagePaths;
            new ProcessQuadData()
                .LoadQuadJson(_quadFilePath)
                .ProcessJson();

            Console.WriteLine("Process Complete!");
        });
    }
    [RelayCommand]
    private void AddNewElement() => Elements.Add(new ElementViewModel(vm => Elements.RemoveAt(vm.Index)));

}