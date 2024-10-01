﻿using QuadToSpine2D.Core.Process;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core;

public class Pool
{
    private readonly Dictionary<string, List<PoolData>> _poolDictionary = new();

    private readonly ProcessImages  _processImages = new(GlobalData.ImagePath);
    private readonly List<PoolData> _unused        = [];
    private readonly List<PoolData> _used          = [];

    public PoolData Get(KeyframeLayer layer)
    {
        _poolDictionary.TryGetValue(layer.Guid, out var poolsData);
        PoolData? usedPoolData   = null;
        PoolData? unusedPoolData = null;

        for (var index = 0; index < poolsData?.Count; index++)
        {
            unusedPoolData ??= _unused.Find(x => x == poolsData[index]);
            usedPoolData   ??= _used.Find(x => x   == poolsData[index]);
            if (unusedPoolData is not null && usedPoolData is not null) break;
        }

        var poolData = unusedPoolData ?? Create(layer, usedPoolData);
        _used.Add(poolData);
        return poolData;
    }

    private PoolData Create(KeyframeLayer layer, PoolData? usedPoolData)
    {
        var copyIndex = 0;
        if (usedPoolData is not null)
            copyIndex = usedPoolData.LayersData.Count;
        var data     = _processImages.GetLayerData(layer, usedPoolData, copyIndex);
        
        var poolData = new PoolData { LayersData = data };
        
        if (!_poolDictionary.TryGetValue(layer.Guid, out var value))
            _poolDictionary.Add(layer.Guid, [poolData]);
        else
            value.Add(poolData);
    
        return poolData;
    }

    public void Release(KeyframeLayer layer)
    {
        var poolData = FindPoolData(layer);
        _used.Remove(poolData);
        _unused.Add(poolData);
    }
    public void Release(PoolData poolData)
    {
        _used.Remove(poolData);
        _unused.Add(poolData);
    }

    public PoolData FindPoolData(KeyframeLayer layer)
    {
        // bugs: if there are multiple layers with the same guid, this method will return the first one it finds
        var poolsData = _poolDictionary[layer.Guid];
        foreach (var poolData in poolsData)
        {
            if (_used.Contains(poolData))
                return poolData;
        }

        throw new ArgumentException("Pool data not found for layer " + layer.Guid);
    }

    public void ReleaseAll()
    {
        _unused.AddRange(_used);
        _used.Clear();
    }
}