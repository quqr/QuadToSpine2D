namespace QTSCore.Data;

[Obsolete]
public static class GlobalData
{
    public const float Fps = 1 / 60f;
    private static int _scaleFactor = 1;
    
    public static int  FogTexId           => 1000;

    /// <summary>
    ///     The image may be bigger than original size
    /// </summary>
    public static int ScaleFactor
    {
        get => _scaleFactor;
        set => _scaleFactor = value > 1 ? value : 1;
    }

    public static List<List<string?>> ImagePath { get; set; } =
        [];

    public static string ImageSavePath { get; set; } = @"F:\Codes\Test\output";

    public static string ResultSavePath { get; set; } = @"F:\Codes\Test\output";

    public static bool IsReadableJson     { get; set; }
    public static bool IsSetLoopAnimation { get; set; }

    public static void InitializeUiResources()
    {
    }
}