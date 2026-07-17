namespace QTSCore.Data.Spine;

public class Boundingbox : BaseMesh
{
    public string Type { get; set; } = "boundingbox";
    public int VertexCount { get; set; } = 4;
    public float[] Vertices { get; set; } = new float[8];
}