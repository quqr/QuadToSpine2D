using QTSCore.Data;

namespace QTSCore.Process;

/// <summary>
/// 表示池中的层数据容器
/// </summary>
/// <remarks>
/// 此类用于管理关键帧层的数据复用，避免重复创建相同的数据结构。
/// 每个PoolData实例包含一组LayerData，并关联一个FramePoint表示其生命周期。
/// </remarks>
public class PoolData
{
    private FramePoint _framePoint = new(-1);

    /// <summary>
    /// 获取或初始化层数据列表
    /// </summary>
    /// <value>
    /// 包含所有关联LayerData实例的列表
    /// </value>
    public required List<LayerData> LayersData { get; init; }

    /// <summary>
    /// 获取或设置帧点信息
    /// </summary>
    /// <value>
    /// 表示此池数据有效帧范围的FramePoint结构
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// 当尝试设置新的FramePoint但当前FramePoint的EndFrame不等于-1时抛出
    /// </exception>
    public FramePoint FramePoint
    {
        get => _framePoint;
        set
        {
            if (_framePoint.EndFrame != -1 && value.EndFrame != -1)
                throw new InvalidOperationException("FramePoint is already set. Something went wrong.");
            _framePoint = value;
        }
    }
}

/// <summary>
/// 表示帧点的不可变结构，用于定义动画帧的范围
/// </summary>
/// <remarks>
/// FramePoint是一个值类型，用于标识动画中的特定帧范围。
/// 它实现了IEquatable接口以支持相等性比较。
/// </remarks>
public readonly struct FramePoint : IEquatable<FramePoint>
{
    /// <summary>
    /// 获取起始帧索引
    /// </summary>
    public int StartFrame { get; }

    /// <summary>
    /// 获取结束帧索引
    /// </summary>
    public int EndFrame { get; }

    /// <summary>
    /// 使用起始帧和结束帧初始化FramePoint实例
    /// </summary>
    /// <param name="startFrame">起始帧索引</param>
    /// <param name="endFrame">结束帧索引</param>
    /// <exception cref="ArgumentException">
    /// 当startFrame大于endFrame时抛出
    /// </exception>
    public FramePoint(int startFrame, int endFrame)
    {
        if (startFrame > endFrame) throw new ArgumentException("End frame must be greater than start frame.");
        StartFrame = startFrame;
        EndFrame = endFrame;
    }

    /// <summary>
    /// 使用单一帧索引初始化FramePoint实例
    /// </summary>
    /// <param name="frame">帧索引，同时作为起始帧和结束帧</param>
    public FramePoint(int frame)
    {
        StartFrame = frame;
        EndFrame = frame;
    }

    /// <summary>
    /// 比较两个FramePoint实例是否相等
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>如果两个实例相等则返回true，否则返回false</returns>
    public static bool operator ==(FramePoint left, FramePoint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 比较两个FramePoint实例是否不相等
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>如果两个实例不相等则返回true，否则返回false</returns>
    public static bool operator !=(FramePoint left, FramePoint right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// 判断当前实例是否与另一个FramePoint相等
    /// </summary>
    /// <param name="other">要比较的FramePoint实例</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public bool Equals(FramePoint other)
    {
        return StartFrame == other.StartFrame && EndFrame == other.EndFrame;
    }

    /// <summary>
    /// 判断当前实例是否与指定对象相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果对象是FramePoint且相等则返回true，否则返回false</returns>
    public override bool Equals(object? obj)
    {
        return obj is FramePoint other && Equals(other);
    }

    /// <summary>
    /// 获取当前实例的哈希码
    /// </summary>
    /// <returns>基于StartFrame和EndFrame计算的哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(StartFrame, EndFrame);
    }
}
