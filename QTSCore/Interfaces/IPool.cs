using QTSCore.Data.Quad;
using QTSCore.Process;

namespace QTSCore.Interfaces;

public interface IPool
{
    Dictionary<string, List<PoolData>> UsedPoolDictionary { get; }
    PoolData Get(KeyframeLayer layer);
    void Release(KeyframeLayer layer, PoolData poolData);
    PoolData FindPoolData(KeyframeLayer layer, FramePoint framePoint);
}