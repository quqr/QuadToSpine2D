namespace QuadToSpine2D.Core.Process;

public class PoolData
{
    private FramePoint      _framePoint = new(-1);
    public  List<LayerData> LayersData { get; init; }

    public FramePoint FramePoint
    {
        get => _framePoint;
        set
        {
            if (_framePoint.EndFrame != -1 && value.EndFrame != -1)
                throw new InvalidOperationException("FramePoint is already set.");
            _framePoint = value;
        }
    }
}

public readonly struct FramePoint : IEquatable<FramePoint>
{
    public int StartFrame { get; }
    public int EndFrame   { get; }

    public FramePoint(int startFrame, int endFrame)
    {
        StartFrame = startFrame;
        EndFrame   = endFrame;
    }

    public FramePoint(int frame)
    {
        StartFrame = frame;
        EndFrame   = frame;
    }

    public static bool operator ==(FramePoint left, FramePoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FramePoint left, FramePoint right)
    {
        return !left.Equals(right);
    }

    public bool Equals(FramePoint other)
    {
        return StartFrame == other.StartFrame && EndFrame == other.EndFrame;
    }

    public override bool Equals(object? obj)
    {
        return obj is FramePoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartFrame, EndFrame);
    }
}