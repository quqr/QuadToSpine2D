namespace QuadToSpine2D.Core.Process;

/// <summary>
/// Pool class to manage the pool of layer data.
/// A keyframe layer can have multiple copies of same layers, and they must be saved,
/// Spine2D can not use same images to render at the same time.
/// To prevent much same image be saved, we use a pool to manage the layer data.And reuse it when needed.
/// </summary>
public class Pool
{
    private readonly Dictionary<string, List<PoolData>> _poolDictionary = new();

    private readonly ProcessImages                      _processImages        = new(GlobalData.ImagePath);
    private readonly Dictionary<string, List<PoolData>> _unusedPoolDictionary = new();

    public Dictionary<string, List<PoolData>> UsedPoolDictionary { get; } = new();

    public PoolData Get(KeyframeLayer layer)
    {
        if (!UsedPoolDictionary.TryGetValue(layer.Guid, out var usedPoolsData))
            UsedPoolDictionary.Add(layer.Guid, usedPoolsData = []);
        if (!_unusedPoolDictionary.TryGetValue(layer.Guid, out var unusedPoolsData))
            _unusedPoolDictionary.Add(layer.Guid, unusedPoolsData = []);

        var unusedPoolData = unusedPoolsData.FirstOrDefault();

        PoolData poolData;
        if (unusedPoolData is null)
        {
            poolData = Create(layer, usedPoolsData);
        }
        else
        {
            poolData = unusedPoolData;
            unusedPoolsData.Remove(poolData);
        }

        usedPoolsData.Add(poolData);
        return poolData;
    }

    private PoolData Create(KeyframeLayer layer, List<PoolData> usedPoolsData)
    {
        var       copyIndex    = usedPoolsData.Count;
        PoolData? usedPoolData = null;
        if (usedPoolsData.Count != 0)
            usedPoolData = usedPoolsData[0];
        if (copyIndex > 150)
            throw new InvalidOperationException("Too many copies of layer data. Please increase the limit.");
        var data = _processImages.GetLayerData(layer, usedPoolData, copyIndex);

        var poolData = new PoolData { LayersData = data };

        if (!_poolDictionary.TryGetValue(layer.Guid, out var value))
            _poolDictionary.Add(layer.Guid, [poolData]);
        else
            value.Add(poolData);

        return poolData;
    }

    public void Release(KeyframeLayer layer, PoolData poolData)
    {
        poolData.FramePoint = new FramePoint(-1);
        UsedPoolDictionary[layer.Guid].Remove(poolData);
        _unusedPoolDictionary[layer.Guid].Add(poolData);
    }

    public PoolData FindPoolData(KeyframeLayer layer, FramePoint framePoint)
    {
        foreach (var poolData in _poolDictionary[layer.Guid])
            if (UsedPoolDictionary[layer.Guid].Contains(poolData) && poolData.FramePoint == framePoint)
                return poolData;
        throw new ArgumentException("Pool data not found for layer " + layer.Guid);
    }
}