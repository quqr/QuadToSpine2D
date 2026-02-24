using QTSAvalonia.Helper;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.JsonConverters;
using QTSCore.Process;
using QTSCore.Utility;
using SkiaSharp;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Data.Quad;

public class QuadJsonData
{
    public Keyframe?[] Keyframe { get; set; } = [];
    public Animation?[] Animation { get; set; } = [];
    public QuadSkeleton?[] Skeleton { get; set; } = [];
    public Slot[] Slot { get; set; } = [];
    public Hitbox?[] Hitbox { get; set; } = [];
    public Blend[] Blend { get; set; } = [];
    public Mix[] Mix { get; set; } = [];
    public Link[] Link { get; set; } = [];
    
}

public class Blend
{
    public string Name { get; set; } = string.Empty;
    public string[] ModeRgb { get; set; } = [];
    public string[] ModeAlpha { get; set; } = [];
    public string Color { get; set; } = string.Empty;
    [JsonIgnore]
    public string[] LogicOp { get; set; } = [];
}

public class Mix
{
    //TODO
}

public class Link
{
    //TODO
}

[JsonConverter(typeof(HitboxJsonConverter))]
public class Hitbox : Attach
{
    public string Name { get; set; } = string.Empty;
    public HitboxLayer[] Layer { get; set; }=[];
}

public class HitboxLayer
{
    public string Name { get; set; } = string.Empty;
    public float[] Hitquad { get; set; } = [];
}

[JsonConverter(typeof(SlotJsonConverter))]
public class Slot : Attach
{
    public Attach[]? Attaches { get; set; }
}

[JsonConverter(typeof(KeyframeJsonConverter))]
public class Keyframe : Attach
{
    public string Name { get; set; } = string.Empty;

    public KeyframeLayer?[]? Layers { get; set; } = [];
    public int[] Order { get; set; } = [];
}

[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer : Attach
{
    private float[]? _srcquad = [];

    public float[] Dstquad
    {
        get;
        set
        {
            DstMatrix = new Matrix(4, 2, value);
            //Y is down, so we need to flip it to up
            field = value;
        }
    } = [];

    public Matrix DstMatrix { get; set; }

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            if (_srcquad is null || _srcquad.Length < 8)
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
    public int BlendId { get; set; }

    public int TexId
    {
        get;
        set
        {
            if (value >= -1)
            {
                field = value;
                return;
            }
            // fog tex id
            field = ConverterSettingViewModel.FogTexId;
        }
    }

    public string Guid { get; set; } = string.Empty;
    public float Height { get; set; }
    public float Width { get; set; }
    public float SrcX { get; set; }
    public float SrcY { get; set; }
    public float[] MinAndMaxSrcPoints { get; set; } = new float[8];
    public float[] UVs { get; set; } = new float[8];
    public float[] ZeroCenterPoints { get; set; } = new float[8];
    public string LayerName { get; set; } = string.Empty;
    public string[] Fog { get; set; } = [];
    public string[] Attribute { get; set; } = [];
    public string Colorize { get; set; } = string.Empty;

    private void CalculateGuid()
    {
        MinAndMaxSrcPoints = ProcessUtility.FindMinAndMaxPoints(_srcquad);
        Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
        Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
        Guid = $"{TexId}_{_srcquad!
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
        Vector2[] uvs = [new(0, 0), new(0, 1), new(1, 0), new(1, 1)];
        var orderPoints = points.OrderBy(a => a.X).ThenBy(b => b.Y).ToList();
        for (var i = 0; i < 4; i++)
        {
            UVs[(int)orderPoints[i].Z * 2] = uvs[i].X;
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
    public string Name
    {
        get;
        set
        {
            field = value;
            var splitName = field.Split(' ');
            // avoid "ALL KEYFRAMES"
            if (!splitName[0].Equals("animation")) return;
            Id = Convert.ToInt32(splitName[^1]);
            AttachType = AttachType.Animation;
        }
    } = string.Empty;

    public Timeline[] Timeline
    {
        get;
        set
        {
            for (var i = 0; i < value.Length; i++)
            {
                value[i].Prev = i > 0 ? value[i - 1] : null;
                value[i].Next = i < value.Length - 1 ? value[i + 1] : null;
            }

            field = value;
        }
    } = [];

    public bool IsLoop { get; set; }

    [JsonProperty]
    public int LoopId
    {
        get;
        set
        {
            IsLoop = value >= 0;
            field = value;
        }
    }
}

public class Timeline
{
    public Timeline? Prev
    {
        get;
        set
        {
            field = value;
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
            AnimationMatrix = AnimationMatrix,
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
                    LoggerHelper.Info($"Can not process attach type : {value}");
                    break;
            }
        }
    }

    public AttachType AttachType { get; set; }
    public int Id { get; set; } = -1;
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
    KeyframeLayer,
    None
}

[JsonConverter(typeof(SkeletonJsonConverter))]
public class QuadSkeleton : Attach
{
    public string Name { get; set; } = string.Empty;
    public QuadBone[]? Bone { get; set; } = [];
    [JsonIgnore] public AnimationData CombineAnimation { get; set; } = new();
}

public class QuadBone
{
    public Attach Attach { get; set; } = new();
}