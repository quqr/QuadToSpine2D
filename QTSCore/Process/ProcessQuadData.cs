using QTSAvalonia.Helper;
using QTSCore.Data.Quad;

namespace QTSCore.Process;

public class ProcessQuadData
{
    public QuadJsonData? QuadData { get; private set; }

    public void ProcessJson()
    {
        if (QuadData is null)
            throw new ArgumentException("Please select correct Quad file");

        var spineJson = new ProcessSpine2DJson(QuadData);
        var outputPath = spineJson.Process().WriteToJson();
        Instances.Converter.Progress = 100;
        Instances.Converter.ResultJsonUrl = outputPath;
        Instances.Converter.ResultJsonUrlIsEnable = true;
    }

    public ProcessQuadData LoadQuadJson(string quadPath, bool isPostProcess = false)
    {
        QuadData = new ProcessQuadJsonFile().LoadQuadJson(quadPath, isPostProcess);
        return this;
    }
}