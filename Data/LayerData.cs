using QuadToSpine.Data.Quad;

namespace QuadToSpine.Data;

public class LayerData
{
    public float[] UVs { get; set; }
    public int CurrentImageIndex { get; set; }
    public string ImageName { get; set; }
    public string SkinName { get; set; }
    public float[] ZeroCenterPoints { get; set; }
    public KeyframeLayer KeyframeLayer { get; set; }
}