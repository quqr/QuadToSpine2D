namespace QuadToSpine2D.Core.Process;

public static class Process
{
    private static QuadJson? _quadData;
    public static void ProcessJson(List<List<string?>> imagePath)
    {
        if (_quadData is null)
        {
            throw new ArgumentException("Please select correct Quad file");
        }
        
        if (imagePath.Count == 0)
        {
            throw new ArgumentException("Please select correct image");
        }
        
        var imageQuad = new ProcessImage();
        var spineJson = new ProcessSpineJson();
        
        imageQuad.Process(imagePath, _quadData);
        spineJson
            .Process(imageQuad, _quadData)
            .WriteToJson();

        GlobalData.LabelContent = GlobalData.ResultSavePath;
        Console.WriteLine(GlobalData.ResultSavePath);

    }

    public static void LoadQuadJson(string quadPath)
    {
        _quadData = new ProcessQuadFile().LoadQuadJson(quadPath);
    }
}