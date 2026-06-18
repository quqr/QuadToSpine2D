using QTSAvalonia.Helper;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Interfaces;

namespace QTSCore.Process;

/// <summary>
/// 对象池实现类，用于管理和复用PoolData对象
/// </summary>
/// <remarks>
/// <para>
/// Pool类实现了IPool接口，提供对象池功能以优化内存使用和性能。
/// 它维护了已使用和未使用的PoolData对象池，支持对象的获取、释放和查找操作。
/// </para>
/// <para>
/// 对象池模式通过复用对象来减少内存分配和垃圾回收的开销。
/// </para>
/// </remarks>
public class Pool : IPool
{
    private readonly Dictionary<string, List<PoolData>> _poolDictionary = new();
    private readonly Dictionary<string, List<PoolData>> _unusedPoolDictionary = new();
    private readonly ProcessImages _processImages;

    /// <summary>
    /// 获取已使用的池数据字典
    /// </summary>
    public Dictionary<string, List<PoolData>> UsedPoolDictionary { get; } = new();

    /// <summary>
    /// 使用默认图像路径初始化Pool实例
    /// </summary>
    public Pool()
    {
        _processImages = new ProcessImages(Instances.ConverterSetting.ImagePath);
    }

    /// <summary>
    /// 使用指定的图像路径列表初始化Pool实例
    /// </summary>
    /// <param name="imagePaths">图像路径列表的列表</param>
    public Pool(List<List<string?>> imagePaths)
    {
        _processImages = new ProcessImages(imagePaths);
    }

    /// <summary>
    /// 从池中获取或创建PoolData对象
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <returns>获取或创建的PoolData实例</returns>
    public PoolData Get(KeyframeLayer layer)
    {
        if (!UsedPoolDictionary.TryGetValue(layer.Guid, out var usedPoolsData))
        {
            UsedPoolDictionary.Add(layer.Guid, usedPoolsData = []);
        }
        if (!_unusedPoolDictionary.TryGetValue(layer.Guid, out var unusedPoolsData))
        {
            _unusedPoolDictionary.Add(layer.Guid, unusedPoolsData = []);
        }

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

    /// <summary>
    /// 释放PoolData对象回池中
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <param name="poolData">要释放的池数据</param>
    public void Release(KeyframeLayer layer, PoolData poolData)
    {
        poolData.FramePoint = new FramePoint(-1);
        UsedPoolDictionary[layer.Guid].Remove(poolData);
        _unusedPoolDictionary[layer.Guid].Add(poolData);
    }

    /// <summary>
    /// 查找指定帧点的PoolData对象
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <param name="framePoint">帧点</param>
    /// <returns>匹配的PoolData实例</returns>
    /// <exception cref="ArgumentException">当找不到匹配的池数据时抛出</exception>
    public PoolData FindPoolData(KeyframeLayer layer, FramePoint framePoint)
    {
        if (!_poolDictionary.TryGetValue(layer.Guid, out var poolDataList))
            throw new ArgumentException("Pool data not found for layer " + layer.Guid);
        foreach (var poolData in poolDataList)
        {
            if (UsedPoolDictionary[layer.Guid].Contains(poolData) && poolData.FramePoint == framePoint)
            {
                return poolData;
            }
        }
        throw new ArgumentException("Pool data not found for layer " + layer.Guid);
    }

    /// <summary>
    /// 创建新的PoolData对象
    /// </summary>
    /// <param name="layer">关键帧层</param>
    /// <param name="usedPoolsData">已使用的池数据列表</param>
    /// <returns>新创建的PoolData实例</returns>
    private PoolData Create(KeyframeLayer layer, List<PoolData> usedPoolsData)
    {
        var poolData = new PoolData
        {
            LayersData = _processImages.GetLayerData(layer, null, usedPoolsData.Count)
        };
        if (!_poolDictionary.TryGetValue(layer.Guid, out var poolDataList))
        {
            poolDataList = [];
            _poolDictionary.Add(layer.Guid, poolDataList);
        }
        poolDataList.Add(poolData);
        return poolData;
    }
}
