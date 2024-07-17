using Avalonia.Controls;
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
    public static Label Label { get; set; }

    public static string LabelContent
    {
        set { Dispatcher.UIThread.Post(() => { Label.Content = $"State: {value}"; }); }
    }
    public static bool IsReadableJson { get; set; }
}