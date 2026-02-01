using QTSCore.Data.Quad;

namespace QTSCore.Process;

public class ProcessQuadData
{
    public QuadJsonData? QuadData { get; set; }

    public void ProcessJson(List<List<string?>> imagePath)
    {
        if (QuadData is null)
            throw new ArgumentException("Please select correct Quad file");

        if (imagePath.Count == 0)
            throw new ArgumentException("Please select correct image");

        var spineJson = new ProcessSpine2DJson(QuadData);
        spineJson.Process().WriteToJson();
    }

    public ProcessQuadData LoadQuadJson(string quadPath)
    {
        QuadData = new ProcessQuadJsonFile().LoadQuadJson(quadPath);
        return this;
    }
}