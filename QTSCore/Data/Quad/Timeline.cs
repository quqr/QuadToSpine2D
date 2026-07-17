using QTSCore.Process;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Data.Quad;

public class Timeline
{
    private Timeline? _prev;

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

    public int StartFrame { get; set; }

    public int EndFrame { get; set; }

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
            AnimationMatrix = AnimationMatrix
        };
    }
}