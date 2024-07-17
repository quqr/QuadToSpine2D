using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using QuadToSpine2D.Core.Process;
using QuadToSpine2D.Tools;

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
        GlobalData.Label = StateLabel;
    }

    private void BindEvent()
    {
        ProcessButton.Click += ProcessButtonOnClick;
        UploadButton.Click += UploadButtonOnClick;
        AddNewButton.Click += AddNewButtonOnClick;
        ScaleFactorTextBox.TextChanged += ScaleFactorTextBoxOnTextChanged;
    }

    private void ScaleFactorTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ScaleFactorTextBox.Text is null || ScaleFactorTextBox.Text.Equals(string.Empty)) return;
        GlobalData.ScaleFactor = Convert.ToInt32(ScaleFactorTextBox.Text);
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
            Background = new SolidColorBrush(new Color(100, 199, 165, 136)),
            Children =
            {
                label, scrollView, addButton, deleteButton
            }
        };
        var bitmaps = new List<Bitmap>();

        ImageBox.Children.Insert(ImageBox.Children.Count - 1, content);
        var imageIndex = _currentImageBoxPart - 1;

        addButton.Click += AddButtonClick;
        deleteButton.Click += DeleteButtonOnClick;

        return;

        void AddButtonClick(object? o, RoutedEventArgs routedEventArgs)
        {
            var files = Utility.OpenImageFilePicker(StorageProvider);
            if (files is null) return;
            var imageIndex = _currentImageBoxPart - 1;
            foreach (var file in files)
            {
                var bitmap = ImageLoader.LoadImage(file);
                var image = new Image
                {
                    Width = 100,
                    Height = 100,
                    Source = bitmap,
                    Margin = new Thickness(10)
                };
                _imagePath[imageIndex].Add(Utility.ConvertUriToPath(file.Path));
                stackPanel.Children.Add(image);
                bitmaps.Add(bitmap);
                file.Dispose();
            }
        }

        void DeleteButtonOnClick(object? o, RoutedEventArgs routedEventArgs)
        {
            bitmaps.ForEach(x => x.Dispose());
            bitmaps.Clear();
            _imagePath[imageIndex] = null;
            ImageBox.Children.Remove(content);
        }
    }

    private void UploadButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var file = Utility.OpenQuadFilePicker(StorageProvider);
        if (file is null) return;
        IsUploadedCheckBox.IsChecked = true;
        QuadFileNameLabel.Content = file[0].Name;
        _quadFilePath = Utility.ConvertUriToPath(file[0].Path);
        file[0].Dispose();
    }

    private void ProcessButtonOnClick(object? sender, RoutedEventArgs e)
    {
        GlobalData.ImageSavePath = Directory.GetCurrentDirectory();
        GlobalData.ResultSavePath = Path.Combine(GlobalData.ImageSavePath, "images");
        if (!Directory.Exists(GlobalData.ImageSavePath))
        {
            Directory.CreateDirectory(GlobalData.ImageSavePath);
        }
#if DEBUG
        GlobalData.ImageSavePath = @"E:\Asset\ttt\images";
        GlobalData.ResultSavePath = @"E:\Asset\ttt";
#endif
        Task.Run(() =>
        {
            Process
                .ProcessJson(_quadFilePath, Utility.ConvertImagePath(_imagePath));
        });
    }
}