using QuadToSpine2D.Core.Process;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core;

public class LayerDataPool
{
    public readonly List<PoolData> _all = [];
    private readonly List<PoolData> _used = [];
    private readonly List<PoolData> _unused = [];
    private readonly ProcessImages _processImages = new(GlobalData.ImagePath);
    
    public PoolData Get(KeyframeLayer layer)
    {
        PoolData? layerPoolData = null;
        if (_processImages.LayersDataDict.TryGetValue(layer.TexId, out var value)
            && value.ContainsKey(layer.LayerGuid))
            layerPoolData = _processImages.LayersDataDict[layer.TexId][layer.LayerGuid];

        var unusedPoolData = _unused.Find(x => x == layerPoolData);
        var usedPoolData = _used.Find(x => x == layerPoolData);
        var poolData = unusedPoolData ?? Create(layer, usedPoolData);
        _all.Add(poolData);
        _used.Add(poolData);
        return poolData;
    }

    private PoolData Create(KeyframeLayer layer, PoolData? usedPoolData)
    {
        var copyIndex = 0;
        if (usedPoolData is not null) copyIndex = usedPoolData.LayersData[0].Count;
        var poolData = _processImages.GetLayerData(layer, copyIndex);
        return poolData;
    }

    public void Release(PoolData data)
    {
        if (!_used.Contains(data))
        {
            throw new ArgumentException("Data is not used.");
        }
        _used.Remove(data);
        _unused.Add(data);
    }

    public void ReleaseAll()
    {
        _unused.AddRange(_used);
        _used.Clear();
    }
}