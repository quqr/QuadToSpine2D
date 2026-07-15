namespace QTSCore.Data.Spine;

public class Mesh : BaseMesh
{
    public string Type { get; set; } = "mesh";
    public float[] Uvs { get; set; } = new float[8];
    public float[] Triangles { get; set; } = [1, 2, 3, 1, 3, 0];
    public float[] Vertices { get; set; } = new float[8];
    public int Hull { get; set; } = 4;
}
