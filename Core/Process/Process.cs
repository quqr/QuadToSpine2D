namespace QuadToSpine2D.Core.Process;

public static class Process
{
    private static QuadJson? _quadData;
    public static void ProcessJson(List<List<string?>> imagePath)
    {
        //var quadData = new ProcessQuadFile().LoadQuadJson(quadPath);
        var imageQuad = new ProcessImage();
        var spineJson = new ProcessSpineJson();
        if (_quadData is null)
        {
            GlobalData.LabelContent = "Please select Quad file";
            return;
        }
        if (imagePath.Count == 0)
        {
            GlobalData.LabelContent = "Please select correct image";
            return;
        }
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