namespace QuadToSpine2D.Core.Utility;

public class PoolData
{
    public LayerData FirstLayerData => LayersData[0][0];
    public Dictionary<int, List<LayerData>> LayersData { get; set; } = [];
}