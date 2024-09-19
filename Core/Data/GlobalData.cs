using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace QuadToSpine2D.Core.Data;

public static class GlobalData
{
    private static int _scaleFactor = 1;

    public static int ScaleFactor
    {
        get => _scaleFactor;
        set => _scaleFactor = value > 1 ? value : 1;
    }

    public static string ImageSavePath { get; set; } = string.Empty;
    public static string ResultSavePath { get; set; } = string.Empty;
    public static ProgressBar ProcessBar { get; set; } = null!;
    public static ISolidColorBrush ProcessBarNormalBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 128, 128));
    public static ISolidColorBrush ProcessBarErrorBrush { get; set; } = new SolidColorBrush(Color.FromRgb(193, 44, 31));
    public static double BarValue
    {
        get => ProcessBar.Value;
        set
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProcessBar.Value = value;
            }); 
        }
    }
    public static string BarTextContent
    {
        set
        {
            if (!value.Equals(string.Empty))
                //value += "{1}%";
                Dispatcher.UIThread.Post(() =>
                {
                    ProcessBar.ProgressTextFormat = value;
                });
        }
    }
    public static bool IsReadableJson { get; set; }
    public static bool IsRemoveUselessAnimations { get; set; }

    public static bool IsAddBoundingBox { get; set; }
    public static bool IsAddHitBox { get; set; }
}