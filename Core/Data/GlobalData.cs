using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace QuadToSpine2D.Core.Data;

public static class GlobalData
{
    public const float Fps = 1 / 60f;
    private static int _scaleFactor = 1;
    private static List<List<string?>> _imagePath = [];

    private static double _barValue;
    public static bool IsCompleted { get; set; }
    public static bool IsSetLoopAnimation { get; set; }
    public static int FogTexId => 1000;

    public static int ScaleFactor
    {
        get => _scaleFactor;
        set => _scaleFactor = value > 1 ? value : 1;
    }

    public static List<List<string?>> ImagePath
    {
        get => _imagePath;
        set
        {
            _imagePath = [];
            var maxCount = value.Max(x => x.Count);
            for (var i = 0; i < value.Count; i++)
            {
                _imagePath.Add([]);
                for (var j = 0; j < maxCount; j++) _imagePath[i].Add(j >= value[i].Count ? null : value[i][j]);
            }
        }
    }

    public static string ImageSavePath { get; set; } = string.Empty;
    public static string ResultSavePath { get; set; } = string.Empty;
    public static ProgressBar ProcessBar { get; set; } = null!;

    public static ISolidColorBrush ProcessBarNormalBrush { get; set; } =
        new SolidColorBrush(Color.FromRgb(0, 128, 128));

    public static ISolidColorBrush ProcessBarErrorBrush { get; set; } = new SolidColorBrush(Color.FromRgb(193, 44, 31));

    public static double BarValue
    {
        get => _barValue;
        set
        {
            _barValue = value;
            Dispatcher.UIThread.Post(() => { ProcessBar.Value = value; });
        }
    }

    public static string BarTextContent
    {
        set
        {
            if (!value.Equals(string.Empty))
                Dispatcher.UIThread.Post(() => { ProcessBar.ProgressTextFormat = value; });
        }
    }

    public static bool IsReadableJson { get; set; }
}