using QTSCore.Process;
using QTSCore.Utility;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Data.Quad;

public class Timeline
{
    private Timeline? _prev;
    private int _startFrame;
    private int _endFrame;

    public Timeline? Prev
    {
        get => _prev;
        set
        {
            _prev = value;
            StartFrame = value?.EndFrame ?? 0;
            EndFrame = StartFrame + Frames;
        }
    }

    public Timeline? Next { get; set; }
    public int Frames => Time;
    public int Time { get; set; }
    public int StartFrame
    {
        get => _startFrame;
        set => _startFrame = value;
    }
    public int EndFrame
    {
        get => _endFrame;
        set => _endFrame = value;
    }
    public FramePoint FramePoint { get; set; }
    public Attach? Attach { get; set; }

    public string Color { get; set; } = string.Empty;
    public int MatrixMixId { get; set; }
    public int ColorMixId { get; set; }
    public int DstquadMixId { get; set; }
    public int FogquadMixId { get; set; }
    public int SrcquadMixId { get; set; }
    public int HitquadMixId { get; set; }
    public Matrix AnimationMatrix { get; private init; } = Utility.Matrix.IdentityMatrixBy4X4;

    private float[]? Matrix
    {
        init
        {
            if (value is null) return;
            AnimationMatrix = new Matrix(4, 4, value);
        }
    }

    public Timeline Clone()
    {
        return new Timeline
        {
            Prev = Prev,
            Next = Next,
            Time = Time,
            StartFrame = StartFrame,
            EndFrame = EndFrame,
            FramePoint = FramePoint,
            Attach = Attach,
            AnimationMatrix = AnimationMatrix,
        };
    }
}
