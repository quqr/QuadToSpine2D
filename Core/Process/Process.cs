using QuadToSpine.Data;

namespace QuadToSpine.Process;

public static class Process
{
    public static void ProcessJson(string quadPath, List<List<string?>> imagePath)
    {
        var quad = new ProcessQuadFile();
        var imageQuad = new ProcessImage();
        var spineJson = new ProcessSpineJson();

        quad.Load(quadPath);
        imageQuad.Process(imagePath, quad.QuadData);
        spineJson.Process(imageQuad, quad.QuadData);

        GlobalData.LabelContent = GlobalData.ResultSavePath;
    }
}