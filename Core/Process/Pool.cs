namespace QuadToSpine2D.Core.Process;

public class Pool
{
    private readonly Dictionary<string, List<PoolData>> _poolDictionary = new();

    private readonly ProcessImages                      _processImages        = new(GlobalData.ImagePath);
    private readonly Dictionary<string, List<PoolData>> _unusedPoolDictionary = new();
    private readonly Dictionary<string, List<PoolData>> _usedPoolDictionary   = new();

    public PoolData Get(KeyframeLayer layer)
    {
        if (!_usedPoolDictionary.TryGetValue(layer.Guid, out var usedPoolsData))
            _usedPoolDictionary.Add(layer.Guid, usedPoolsData = []);
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
        _usedPoolDictionary[layer.Guid].Remove(poolData);
        _unusedPoolDictionary[layer.Guid].Add(poolData);
    }

    public PoolData FindPoolData(KeyframeLayer layer, FramePoint framePoint)
    {
        foreach (var poolData in _poolDictionary[layer.Guid])
            if (_usedPoolDictionary[layer.Guid].Contains(poolData) && poolData.FramePoint == framePoint)
                return poolData;
        throw new ArgumentException("Pool data not found for layer " + layer.Guid);
    }
}