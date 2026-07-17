namespace QTSCore.Data.Spine;

public class LinkedMesh : BaseMesh
{
    public string Type { get; set; }

    //[JsonIgnore]
    public string Skin { get; set; }

    public string Parent { get; set; }
}