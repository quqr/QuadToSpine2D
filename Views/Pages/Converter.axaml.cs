using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using QTSAvalonia.Utilities;
using QTSCore.Data;
using QTSCore.Process;
using SukiUI.Controls;

namespace QTSAvalonia.Views.Pages;

public partial class Converter : UserControl
{
    private          int                                   _currentImageBoxPart;
    private          string                                _quadFilePath = string.Empty;
    private readonly Dictionary<StackPanel, List<string?>> _buttonStatus  = new();

    public Converter()
    {
        InitializeComponent();
        GlobalData.ProcessBar = ProcessBar;
    }

    private void OpenSettingWindow(object? sender, RoutedEventArgs e)
    {
        //new Settings().ShowDialog(this);
    }

    private void AddNewElement(object? sender, RoutedEventArgs e)
    {
        _currentImageBoxPart++;

        var label = new Label
        {
            Content             = $"Part {_currentImageBoxPart}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var stackPanel = new StackPanel();
        var scrollView = new ScrollViewer
        {
            Content   = stackPanel,
            MaxHeight = 300
        };
        var addButton = new Button
        {
            Content                    = "Add",
            Width                      = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            HorizontalAlignment        = HorizontalAlignment.Center,
            VerticalAlignment          = VerticalAlignment.Center
        };
        var deleteButton = new Button
        {
            Content                    = "Delete",
            Width                      = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            HorizontalAlignment        = HorizontalAlignment.Center,
            VerticalAlignment          = VerticalAlignment.Center
        };
        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Margin              = new Thickness(10),
            Children =
            {
                label, scrollView, addButton, deleteButton
            }
        };
        ImageBox.Children.Insert(ImageBox.Children.Count - 1, content);

        _buttonStatus[content] =  [];
        addButton.Click       += AddButtonClick;
        deleteButton.Click    += DeleteButtonOnClick;

        return;

        void AddButtonClick(object? o, RoutedEventArgs routedEventArgs)
        {
            //var files = Utility.OpenImageFilePicker(StorageProvider);
            // if (files is null) return;
            // foreach (var file in files)
            // {
            //     var hyperLink = new HyperlinkButton
            //     {
            //         Content     = file.Name,
            //         NavigateUri = file.Path
            //     };
            //     _buttonStatus[content].Add(file.Path.DecodePath());
            //     stackPanel.Children.Add(hyperLink);
            //     file.Dispose();
            // }
        }

        void DeleteButtonOnClick(object? o, RoutedEventArgs routedEventArgs)
        {
            ImageBox.Children.Remove(content);
            _buttonStatus.Remove(content);
        }
    }

    private void OpenQuadFile(object? sender, RoutedEventArgs e)
    {
        // var file = Utility.OpenQuadFilePicker(StorageProvider);
        // if (file is null) return;
        // _quadFilePath             = file[0].Path.DecodePath();
        // QuadFileNameLabel.Content = file[0].Name;
        // file[0].Dispose();
    }

    private void ProcessData(object? sender, RoutedEventArgs e)
    {
        return;
        ProcessButton.IsEnabled = false;

        GlobalData.ResultSavePath = Directory.GetCurrentDirectory();
        GlobalData.ImageSavePath  = Path.Combine(GlobalData.ResultSavePath, "images");
#if DEBUG
        GlobalData.ImageSavePath = @"E:\Asset\tt\images";
        GlobalData.ResultSavePath = @"E:\Asset\tt";
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        List<List<string?>> imagePath =
        [
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.0.gnf.png"
            ],
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.1.gnf.png"
            ],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"]
        ];
#endif
        if (!Directory.Exists(GlobalData.ImageSavePath)) Directory.CreateDirectory(GlobalData.ImageSavePath);
#if DEBUG
        Directory.Delete(GlobalData.ImageSavePath, true);
        Directory.CreateDirectory(GlobalData.ImageSavePath);
#endif
        ResultJsonUriButton.Content   = string.Empty;
        ResultJsonUriButton.IsEnabled = false;
        
        ProcessBar.Value       = 0;
        ProcessBar.Foreground  = GlobalData.ProcessBarNormalBrush;
        GlobalData.IsCompleted = true;
        Task.Run(() =>
        {
#if RELEASE
            var imagePath = new List<List<string?>>();
            foreach (var part in _buttonStatus)
            {
                var imageList = new List<string?>();
                foreach (var image in part.Value) imageList.Add(image);
                imagePath.Add(imageList);
            }
#endif            
            GlobalData.ImagePath = imagePath;
            
            new ProcessQuadData()
               .LoadQuadJson(_quadFilePath)
               .ProcessJson(GlobalData.ImagePath);

            Console.WriteLine("Process Complete!");
            if (!GlobalData.IsCompleted)
            {
                throw new InvalidOperationException("Process is not completed.");
            }
            Dispatcher.UIThread.Post(() =>
            {
                
                GlobalData.BarValue       = 100;
                GlobalData.BarTextContent = "completed !";

                ResultJsonUriButton.IsEnabled   = true;
                ResultJsonUriButton.Content     = GlobalData.ResultSavePath;
                ResultJsonUriButton.NavigateUri = new Uri(GlobalData.ResultSavePath);
                ProcessButton.IsEnabled         = true;
            });
        }).ContinueWith(task =>
        {
            if (task.Exception?.InnerException is null) return;
            Console.WriteLine(task.Exception.InnerException.Message);
            Dispatcher.UIThread.Post(() =>
            {
                GlobalData.BarValue              = 100;
                GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
                GlobalData.BarTextContent        = task.Exception.InnerException.Message;
                ProcessButton.IsEnabled          = true;
            });
        });
    }
}