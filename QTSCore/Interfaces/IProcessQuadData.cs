using QTSCore.Data.Quad;

namespace QTSCore.Interfaces;

public interface IProcessQuadData
{
    QuadJsonData? QuadData { get; }
    IProcessQuadData LoadQuadJson(string quadPath, bool isPostProcess = false);
    void ProcessJson();
}
