using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using QuadToSpine2D.AvaUtility;
using QuadToSpine2D.Core.Process;
using System;

namespace QuadToSpine2D;

public partial class MainWindow : Window
{
    private int _currentImageBoxPart;
    private string _quadFilePath = string.Empty;
    private List<List<string?>?> _imagePath = [];

    public MainWindow()
    {
        InitializeComponent();
        BindEvent();
        GlobalData.ProcessBar = ProcessBar;
    }

    private void BindEvent()
    {
        ProcessButton.Click += ProcessButtonOnClick;
        UploadButton.Click += UploadQuadFileButtonOnClick;
        AddNewButton.Click += AddNewButtonOnClick;
        ScaleFactorTextBox.TextChanged += ScaleFactorTextBoxOnTextChanged;
        ReadableCheckBox.IsCheckedChanged += ReadableCheckBoxOnClick;
        RemoveAnimationCheckBox.IsCheckedChanged += RemoveAnimationCheckBoxOnIsCheckedChanged;
        AddBoundingBoxCheckBox.IsCheckedChanged += AddBoundingBoxCheckBoxOnIsCheckedChanged;
    }

    private void AddBoundingBoxCheckBoxOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (AddBoundingBoxCheckBox.IsChecked != null)
            GlobalData.IsAddBoundingBox = (bool)AddBoundingBoxCheckBox.IsChecked;
    }

    private void RemoveAnimationCheckBoxOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (RemoveAnimationCheckBox.IsChecked != null)
            GlobalData.IsRemoveUselessAnimations = (bool)RemoveAnimationCheckBox.IsChecked;
    }

    private void ReadableCheckBoxOnClick(object? sender, RoutedEventArgs e)
    {
        if (ReadableCheckBox.IsChecked != null)
            GlobalData.IsReadableJson = (bool)ReadableCheckBox.IsChecked;
    }

    private void ScaleFactorTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ScaleFactorTextBox.Text is null || ScaleFactorTextBox.Text.Equals(string.Empty)) return;
        try
        {
            GlobalData.ScaleFactor = Convert.ToInt32(ScaleFactorTextBox.Text);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            GlobalData.BarValue = 100;
            GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
            GlobalData.BarTextContent = exception.Message;
            ScaleFactorTextBox.Text = "1";
        }
    }

    private void AddNewButtonOnClick(object? sender, RoutedEventArgs e)
    {
        _currentImageBoxPart++;
        _imagePath.Add([]);

        var label = new Label()
        {
            Content = $"Part {_currentImageBoxPart}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var stackPanel = new StackPanel();
        var scrollView = new ScrollViewer()
        {
            Content = stackPanel,
            MaxHeight = 300
        };
        var addButton = new Button()
        {
            Content = "Add",
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var deleteButton = new Button()
        {
            Content = "Delete",
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var content = new StackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10),
            Children =
            {
                label, scrollView, addButton, deleteButton
            }
        };
        //var bitmaps = new List<Bitmap>();
        ImageBox.Children.Insert(ImageBox.Children.Count - 1, content);
        var imageIndex = _currentImageBoxPart - 1;

        addButton.Click += AddButtonClick;
        deleteButton.Click += DeleteButtonOnClick;

        return;

        void AddButtonClick(object? o, RoutedEventArgs routedEventArgs)
        {
            var files = Utility.OpenImageFilePicker(StorageProvider);
            if (files is null) return;
            foreach (var file in files)
            {
                // var bitmap = ImageLoader.LoadImage(file);
                // var image = new Image
                // {
                //     Width = 100,
                //     Height = 100,
                //     Source = bitmap,
                //     Margin = new Thickness(10)
                // };
                var hyperLink = new HyperlinkButton
                {
                    Content = file.Name,
                    NavigateUri = file.Path,
                };
                _imagePath[imageIndex] ??= [];
                _imagePath[imageIndex]?.Add(file.Path.DecodePath());
                stackPanel.Children.Add(hyperLink);
                //bitmaps.Add(bitmap);
                file.Dispose();
            }
        }

        void DeleteButtonOnClick(object? o, RoutedEventArgs routedEventArgs)
        {
            // bitmaps.ForEach(x => x.Dispose());
            // bitmaps.Clear();
            _imagePath[imageIndex] = null;
            ImageBox.Children.Remove(content);
        }
    }

    private void UploadQuadFileButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var file = Utility.OpenQuadFilePicker(StorageProvider);
        if (file is null) return;
        QuadFileNameLabel.Content = file[0].Name;
        _quadFilePath = file[0].Path.DecodePath();
        file[0].Dispose();
    }

    private void ProcessButtonOnClick(object? sender, RoutedEventArgs e)
    {
        
        GlobalData.ResultSavePath = Directory.GetCurrentDirectory();
        GlobalData.ImageSavePath = Path.Combine(GlobalData.ResultSavePath, "images");
#if DEBUG
        GlobalData.ImageSavePath = @"E:\Asset\tt\images";
        GlobalData.ResultSavePath = @"E:\Asset\tt";
        _quadFilePath = @"E:\Asset\momohime\4k\00Files\file\Momohime_Rest.mbs.v55.quad";
        _imagePath = [
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.0.tpl1.png"],
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.1.tpl.png"],
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.2.tpl.png"]
        ];
        // _quadFilePath = @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
        // _imagePath = 
        // [
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.0.nvt.png"],
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.1.nvt.png"],
        // ];        
        // _quadFilePath = @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        // _imagePath = 
        // [
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.0.gnf.png"],
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.1.gnf.png"],
        //     [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"],
        // ];
#endif
        if (!Directory.Exists(GlobalData.ImageSavePath))
        {
            Directory.CreateDirectory(GlobalData.ImageSavePath);
        }
        ResultJsonUriButton.Content = string.Empty;
        ResultJsonUriButton.IsEnabled = false;

        ProcessBar.Value = 0;
        ProcessBar.Foreground = GlobalData.ProcessBarNormalBrush;

        Task.Run(() =>
        {
            new ProcessQuadData()
                .LoadQuadJson(_quadFilePath)
                .ProcessJson(Utility.ConvertImagePath(_imagePath));
            Console.WriteLine("Process Complete!");
            Dispatcher.UIThread.Post(() =>
            {
                GlobalData.BarTextContent = string.Empty;
                GlobalData.BarValue = 100;
                ResultJsonUriButton.IsEnabled = true;
                ResultJsonUriButton.Content = GlobalData.ResultSavePath;
                ResultJsonUriButton.NavigateUri = new Uri(GlobalData.ResultSavePath);
            });
        }).ContinueWith((task) =>
        {
            if(task.Exception?.InnerException is null) return;
            Console.WriteLine(task.Exception.InnerException.Message);

            GlobalData.BarValue = 100;
            GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
            GlobalData.BarTextContent = task.Exception.InnerException.Message;
        });
    }
}