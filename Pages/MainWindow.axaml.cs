using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using QuadToSpine2D.AvaUtility;
using QuadToSpine2D.Core.Process;

namespace QuadToSpine2D.Pages;

public partial class MainWindow : Window
{
    private readonly Dictionary<StackPanel, List<string?>> _buttonStatus = new();
    private int _currentImageBoxPart;
    private List<List<string?>> _imagePath = [];
    private string _quadFilePath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        GlobalData.ProcessBar = ProcessBar;
    }

    private void OpenSettingWindow(object? sender, RoutedEventArgs e)
    {
        new Settings().ShowDialog(this);
    }

    private void AddNewElement(object? sender, RoutedEventArgs e)
    {
        _currentImageBoxPart++;

        var label = new Label
        {
            Content = $"Part {_currentImageBoxPart}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var stackPanel = new StackPanel();
        var scrollView = new ScrollViewer
        {
            Content = stackPanel,
            MaxHeight = 300
        };
        var addButton = new Button
        {
            Content = "Add",
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var deleteButton = new Button
        {
            Content = "Delete",
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10),
            Children =
            {
                label, scrollView, addButton, deleteButton
            }
        };
        ImageBox.Children.Insert(ImageBox.Children.Count - 1, content);

        _buttonStatus[content] = [];
        addButton.Click += AddButtonClick;
        deleteButton.Click += DeleteButtonOnClick;

        return;

        void AddButtonClick(object? o, RoutedEventArgs routedEventArgs)
        {
            var files = Utility.OpenImageFilePicker(StorageProvider);
            if (files is null) return;
            foreach (var file in files)
            {
                var hyperLink = new HyperlinkButton
                {
                    Content = file.Name,
                    NavigateUri = file.Path
                };
                _buttonStatus[content].Add(file.Path.DecodePath());
                stackPanel.Children.Add(hyperLink);
                file.Dispose();
            }
        }

        void DeleteButtonOnClick(object? o, RoutedEventArgs routedEventArgs)
        {
            ImageBox.Children.Remove(content);
            _buttonStatus.Remove(content);
        }
    }

    private void OpenQuadFile(object? sender, RoutedEventArgs e)
    {
        var file = Utility.OpenQuadFilePicker(StorageProvider);
        if (file is null) return;
        _quadFilePath = file[0].Path.DecodePath();
        QuadFileNameLabel.Content = file[0].Name;
        file[0].Dispose();
    }

    private void ProcessData(object? sender, RoutedEventArgs e)
    {
        ProcessButton.IsEnabled = false;

        GlobalData.ResultSavePath = Directory.GetCurrentDirectory();
        GlobalData.ImageSavePath = Path.Combine(GlobalData.ResultSavePath, "images");
#if DEBUG
        GlobalData.ImageSavePath = @"E:\Asset\tt\images";
        GlobalData.ResultSavePath = @"E:\Asset\tt";

        // _quadFilePath = @"E:\Asset\momohime\4k\00Files\file\Momohime_Rest.mbs.v55.quad";
        // _imagePath =
        // [
        //     [
        //         @"E:\Asset\momohime\4k\00Files\file\Momohime.0.tpl1.png"
        //     ],
        //     [
        //         @"E:\Asset\momohime\4k\00Files\file\Momohime.1.tpl.png",
        //         @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.1.tpl.png"
        //     ],
        //     [
        //         @"E:\Asset\momohime\4k\00Files\file\Momohime.2.tpl.png",
        //         @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.2.tpl.png"
        //     ]
        // ];
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
        _imagePath =
        [
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.0.nvt.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.1.nvt.png"]
        ];
        // _quadFilePath =
        //     @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        // _imagePath =
        // [
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.0.gnf.png"
        //     ],
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.1.gnf.png"
        //     ],
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"]
        // ];
        // _quadFilePath =
        //     @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M.mbs.v55.quad";
        // _imagePath =
        // [
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.0.nvt.png"
        //     ],
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.1.nvt.png"
        //     ]
        // ];
        // _quadFilePath =
        //     @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Alice.mbs.v55.quad";
        // _imagePath =
        // [
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Alice.0.gnf.png"
        //     ],
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Alice.1.gnf.png"
        //     ],
        //     [
        //         @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Alice.2.gnf.png"
        //     ]
        // ];
        Directory.Delete(GlobalData.ImageSavePath, true);
        Directory.CreateDirectory(GlobalData.ImageSavePath);
        GlobalData.ImagePath = _imagePath;
#endif
        if (!Directory.Exists(GlobalData.ImageSavePath)) Directory.CreateDirectory(GlobalData.ImageSavePath);

#if RELEASE
            var imagePath = new List<List<string?>>();
            foreach (var part in _buttonStatus)
            {
                var imageList = new List<string?>();
                foreach (var image in part.Value) imageList.Add(image);
                imagePath.Add(imageList);
            }
            GlobalData.ImagePath = imagePath;
#endif
        ResultJsonUriButton.Content = string.Empty;
        ResultJsonUriButton.IsEnabled = false;

        ProcessBar.Value = 0;
        ProcessBar.Foreground = GlobalData.ProcessBarNormalBrush;
        GlobalData.IsCompleted = true;
        Task.Run(() =>
        {
            new ProcessQuadData()
                .LoadQuadJson(_quadFilePath)
                .ProcessJson(GlobalData.ImagePath);

            Console.WriteLine("Process Complete!");
            if (!GlobalData.IsCompleted) throw new InvalidOperationException("Process is not completed.");

            Dispatcher.UIThread.Post(() =>
            {
                GlobalData.BarValue = 100;
                GlobalData.BarTextContent = "completed !";

                ResultJsonUriButton.IsEnabled = true;
                ResultJsonUriButton.Content = GlobalData.ResultSavePath;
                ResultJsonUriButton.NavigateUri = new Uri(GlobalData.ResultSavePath);
                ProcessButton.IsEnabled = true;
            });
        }).ContinueWith(task =>
        {
            if (task.Exception?.InnerException is null) return;
            Console.WriteLine(task.Exception.InnerException.Message);
            Dispatcher.UIThread.Post(() =>
            {
                GlobalData.BarValue = 100;
                GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
                GlobalData.BarTextContent = task.Exception.InnerException.Message;
                ProcessButton.IsEnabled = true;
            });
        });
    }
}