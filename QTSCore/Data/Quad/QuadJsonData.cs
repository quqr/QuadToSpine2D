using QTSCore.JsonConverters;
using QTSCore.Process;
using QTSCore.Utility;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Data.Quad;

public class QuadJsonData
{
    public List<Keyframe?>     Keyframe  { get; set; } = [];
    public List<Animation?>    Animation { get; set; } = [];
    public List<QuadSkeleton?> Skeleton  { get; set; } = [];
    public List<Slot>          Slot      { get; set; } = [];
    public List<Hitbox?>       Hitbox    { get; set; } = [];
}

[JsonConverter(typeof(HitboxJsonConverter))]
public class Hitbox : Attach
{
    public string            Name  { get; set; }
    public List<HitboxLayer> Layer { get; set; }
}

public class HitboxLayer
{
    public string  Name    { get; set; }
    public float[] Hitquad { get; set; }
}

[JsonConverter(typeof(SlotJsonConverter))]
public class Slot : Attach
{
    public List<Attach>? Attaches { get; set; }
}

[JsonConverter(typeof(KeyframeJsonConverter))]
public class Keyframe : Attach
{
    public string Name { get; set; }= "";

    public List<KeyframeLayer?>? Layers { get; set; } = [];
    public List<int> Order { get; set; } = [];
}

[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer
{
    private float[] _dstquad=[];
    private float[]? _srcquad=[];

    public float[] Dstquad
    {
        get => _dstquad;
        set
        {
            DstMatrix = new Matrix(4, 2, value);
            //Y is down, so we need to flip it to up
            _dstquad = value;
        }
    }

    public Matrix DstMatrix { get; set; }

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            if (_srcquad is null)
            {
                Guid = $"Fog_{Fog[0]}_{Fog[1]}_{Fog[2]}_{Fog[3]}";
                return;
            }

            CalculateGuid();
            CalculateUVs(_srcquad);

            SrcX = MinAndMaxSrcPoints[0];
            SrcY = MinAndMaxSrcPoints[1];
        }
    }

    public int ImageNameOrder { get; set; }
    public int BlendId        { get; set; }

    public int TexId
    {
        get;
        set
        {
            if (value > -1)
            {
                field = value;
                return;
            }

            // fog tex id
            field = GlobalData.FogTexId;
        }
    }

    public string        Guid               { get; set; } = string.Empty;
    public float         Height             { get; set; }
    public float         Width              { get; set; }
    public float         SrcX               { get; set; }
    public float         SrcY               { get; set; }
    public float[]       MinAndMaxSrcPoints { get; set; } = new float[8];
    public float[]       UVs                { get; set; } = new float[8];
    public float[]       ZeroCenterPoints   { get; set; } = new float[8];
    public string        LayerName          { get; set; } = string.Empty;
    public List<string>  Fog                { get; set; } = [];
    public List<string>? Attribute          { get; set; } = [];

    private void CalculateGuid()
    {
        MinAndMaxSrcPoints = ProcessUtility.FindMinAndMaxPoints(_srcquad);
        Width              = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
        Height             = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
        Guid = $"{TexId}_{_srcquad
                          .Select((t, i) => t * 3.7 / 7.3 + t * i * 97311397.135f / 773377.2746f)
                          .Sum()}";
    }

    /// <summary>
    ///     recalculate UVs
    /// </summary>
    private void CalculateUVs(float[] src)
    {
        List<Vector3> points =
        [
            new(src[0], src[1], 0),
            new(src[2], src[3], 1),
            new(src[4], src[5], 2),
            new(src[6], src[7], 3)
        ];
        Vector2[] uvs         = [new(0, 0), new(0, 1), new(1, 0), new(1, 1)];
        var       orderPoints = points.OrderBy(a => a.X).ThenBy(b => b.Y).ToList();
        for (var i = 0; i < 4; i++)
        {
            UVs[(int)orderPoints[i].Z * 2]     = uvs[i].X;
            UVs[(int)orderPoints[i].Z * 2 + 1] = uvs[i].Y;
        }

        //calculate ZeroCenterPoints, make sure it's in spine2D center in layer picture
        for (var i = 0; i < UVs.Length; i++)
            if (i % 2 == 0)
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Width / 8f;
            else
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Height / 8f;
    }
}

public class Animation : Attach
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            var splitName = _name.Split(' ');
            // avoid "ALL KEYFRAMES"
            if (!splitName[0].Equals("animation")) return;
            Id         = Convert.ToInt32(splitName[^1]);
            AttachType = AttachType.Animation;
        }
    }

    private List<Timeline> _timeline { get; set; }

    public List<Timeline> Timeline
    {
        get => _timeline;
        set
        {
            for (var i = 0; i < value.Count; i++)
            {
                value[i].Prev = i > 0 ? value[i               - 1] : null;
                value[i].Next = i < value.Count - 1 ? value[i + 1] : null;
            }

            _timeline = value;
        }
    }

    public bool IsLoop { get; set; }
    public int  LoopId { get; set; }

    [JsonProperty]
    private int loop_id
    {
        set
        {
            IsLoop = value >= 0;
            LoopId = value;
        }
    }
}

public class Timeline
{
    private Timeline? _prev;

    public Timeline? Prev
    {
        get => _prev;
        set
        {
            _prev      = value;
            StartFrame = value?.EndFrame ?? 0;
            EndFrame   = StartFrame + Frames;
        }
    }

    public Timeline?  Next          { get; set; }
    public int        Frames        => Time;
    public int        Time          { get; set; }
    public int        StartFrame    { get; set; }
    public int        EndFrame      { get; set; }
    public FramePoint FramePoint    { get; set; }
    public Attach?    Attach        { get; set; }
    public bool       IsKeyframeMix { get; private set; }

    [JsonProperty]
    private int keyframe_mix
    {
        set => IsKeyframeMix = value > 0;
    }

    public Matrix AnimationMatrix { get; private set; } = Utility.Matrix.IdentityMatrixBy4X4;

    [JsonProperty]
    private float[]? Matrix
    {
        set
        {
            if (value is null) return;
            var matrix = new Matrix(4, 4, value);
            if (!matrix.IsZeroMatrix())
            {
                AnimationMatrix = matrix;
            }
        }
    }

    public bool IsMatrixMix { get; private set; }

    [JsonProperty]
    private int MatrixMix
    {
        set => IsMatrixMix = value > 0;
    }

    public Timeline Clone()
    {
        return new Timeline
        {
            Prev            = Prev,
            Next            = Next,
            Time            = Time,
            StartFrame      = StartFrame,
            EndFrame        = EndFrame,
            FramePoint      = FramePoint,
            Attach          = Attach,
            IsKeyframeMix   = IsKeyframeMix,
            AnimationMatrix = AnimationMatrix,
            IsMatrixMix     = IsMatrixMix
        };
    }
}

public class Attach
{
    public Attach()
    {
    }

    public Attach(AttachType attachType, int id)
    {
        AttachType = attachType;
        Id = id;
    }

    [JsonProperty]
    private string Type
    {
        set
        {
            switch (value)
            {
                case "keyframe":
                    AttachType = AttachType.Keyframe;
                    break;
                case "slot":
                    AttachType = AttachType.Slot;
                    break;
                case "animation":
                    AttachType = AttachType.Animation;
                    break;
                case "skeleton":
                    AttachType = AttachType.Skeleton;
                    break;
                case "hitbox":
                    AttachType = AttachType.HitBox;
                    break;
                default:
                    Console.WriteLine($"Can not process attach type : {value}");
                    break;
            }
        }
    }

    public AttachType AttachType { get; set; }
    public int        Id         { get; set; } = -1;
}

public enum AttachType
{
    Keyframe,
    Slot,
    HitBox,
    Animation,
    Skeleton,
    Mix,
    List,
    None
}

[JsonConverter(typeof(SkeletonJsonConverter))]
public class QuadSkeleton : Attach
{
    public string          Name             { get; set; }
    public List<QuadBone>? Bone             { get; set; }
    public AnimationData   CombineAnimation { get; set; }
}

public class QuadBone
{
    public Attach Attach { get; set; }
}