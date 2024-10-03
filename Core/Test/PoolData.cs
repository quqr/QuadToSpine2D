namespace QuadToSpine2D.Core.Utility;

public class PoolData
{
    public  List<LayerData> LayersData { get; set; }
    private FramePoint      _framePoint = new(-1);
    public FramePoint FramePoint
    {
        get => _framePoint;
        set
        {
            if (_framePoint.EndFrame != -1 && value.EndFrame != -1)
            {
                throw new InvalidOperationException("FramePoint is already set.");
            }
            _framePoint = value;
        }
    }
}

public struct FramePoint: IEquatable<FramePoint>
{
    public int StartFrame { get; private set; }
    public int EndFrame   { get; private set; }
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
    public static bool operator!=(FramePoint left, FramePoint right)
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