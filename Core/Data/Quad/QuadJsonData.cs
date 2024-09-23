using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using QuadToSpine2D.Core.JsonConverters;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Data.Quad;

public class QuadJsonData
{
    public List<Keyframe?> Keyframe { get; set; } = [];
    public List<Animation?> Animation { get; set; } = [];
    public List<QuadSkeleton?> Skeleton { get; set; } = [];
    public List<Slot> Slot { get; set; } = [];
    public List<Hitbox?> Hitbox { get; set; } = [];

    [MemberNotNull]
    public void RemoveAllNull()
    {
        Skeleton.RemoveAll(x => x is null);
        Animation.RemoveAll(x => x is null || x.Id == -1);

        foreach (var keyframe in Keyframe)
        {
            keyframe?.Layer?.RemoveAll(y => y is null ||
                                            y.LayerGuid.Equals(string.Empty) ||
                                            y.TexId == -1 ||
                                            y.BlendId != 0);
        }

        Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        Parallel.ForEach(Keyframe, keyframe =>
        {
            for (var i = 0; i < keyframe.Layer.Count; i++)
            {
                keyframe.Layer[i].LastLayer = i == 0 ? null : keyframe.Layer[i - 1];
                keyframe.Layer[i].NextLayer = i == keyframe.Layer.Count - 1 ? null : keyframe.Layer[i + 1];
            }
        });
        Parallel.ForEach(Animation, animation =>
        {
            foreach (var timeline in animation.Timeline)
            {
                switch (timeline.Attach?.AttachType)
                {
                    case AttachType.Keyframe:
                        timeline.Attach.Keyframe = Keyframe.Find(x => x.Id == timeline.Attach.Id);
                        break;
                    case AttachType.Slot:
                        var attach = Slot[timeline.Attach.Id].Attaches
                            ?.Find(x => x.AttachType == AttachType.Keyframe);
                        timeline.Attach.Keyframe = Keyframe.Find(x => x.Id == attach?.Id);
                        break;
                    case AttachType.HitBox:
                        timeline.Attach.Hitbox = Hitbox[timeline.Attach.Id];
                        break;
                }
            }
        });
        Hitbox.RemoveAll(x => x is null);
        
        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
    }
}

[JsonConverter(typeof(HitboxJsonConverter))]
public class Hitbox
{
    public string Name { get; set; }
    public List<HitboxLayer> Layer { get; set; }
}

public class HitboxLayer
{
    public string Name { get; set; }
    public float[] Hitquad { get; set; }
}

[JsonConverter(typeof(SlotJsonConverter))]
public class Slot
{
    public List<Attach>? Attaches { get; set; }
}

[JsonConverter(typeof(KeyframeJsonConverter))]
public class Keyframe
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            Id = Convert.ToInt32(_name.Split(' ').Last());
        }
    }

    public int Id { get; set; }
    public List<KeyframeLayer?>? Layer { get; set; }
}

[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer
{
    public KeyframeLayer? LastLayer { get; set; }
    public KeyframeLayer? NextLayer { get; set; }
    private float[] _dstquad;

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
    private float[]? _srcquad;

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            if (value is null) return;
            _srcquad = value;
            MinAndMaxSrcPoints = ProcessUtility.FindMinAndMaxPoints(_srcquad);
            Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
            Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
            LayerGuid = $"{TexId}_{_srcquad
                .Select((t, i) => t * 3.7 / 7.3 + t * i * 97311397.135f / 773377.2746f)
                .Sum()}";
            CalculateUVs(_srcquad);
        }
    }

    public int OrderId { get; set; }
    public int BlendId { get; set; }
    public int TexId { get; set; }
    public string LayerGuid { get; set; } = string.Empty;
    public float Height { get; set; }
    public float Width { get; set; }
    public float[] MinAndMaxSrcPoints { get; set; } = new float[8];
    public float[] UVs { get; set; } = new float[8];
    public float[] ZeroCenterPoints { get; set; } = new float[8];
    public string LayerName { get; set; } = string.Empty;
    public List<string>? Fog { get; set; }
    public List<string>? Attribute { get; set; }

    private void CalculateUVs(float[] src)
    {
        List<Vector3> points =
        [
            new Vector3(src[0], src[1], 0),
            new Vector3(src[2], src[3], 1),
            new Vector3(src[4], src[5], 2),
            new Vector3(src[6], src[7], 3)
        ];
        //Vector2[] uvs = [new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0)];
        Vector2[] uvs = [new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1)];
        var orderPoints = points.OrderBy(a => a.X).ThenBy(b => b.Y).ToList();
        for (var i = 0; i < 4; i++)
        {
            UVs[(int)orderPoints[i].Z * 2] = uvs[i].X;
            UVs[(int)orderPoints[i].Z * 2 + 1] = uvs[i].Y;
        }

        //calculate ZeroCenterPoints
        for (var i = 0; i < UVs.Length; i++)
            if (i % 2 == 0)
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Width / 8f;
            else
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Height / 8f;
    }
}

public class Animation
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            var splitName = _name.Split(' ');
            if (!splitName[0].Equals("animation")) return;

            Id = Convert.ToInt32(splitName.Last());
        }
    }

    public int Id { get; set; } = -1;
    private List<Timeline> _timeline { get; set; }

    public List<Timeline> Timeline
    {
        get => _timeline;
        set
        {
            for (int i = 0; i < value.Count; i++)
            {
                value[i].Next = i < value.Count - 1 ? value[i + 1] : null;
            }

            _timeline = value;
        }
    }

    public bool IsLoop { get; set; }

    [JsonProperty]
    private int loop_id
    {
        set => IsLoop = value >= 0;
    }

    public Animation Clone()
    {
        var animation = new Animation
        {
            Name = Name,
            Id = Id,
            Timeline = Timeline,
            IsLoop = IsLoop
        };
        return animation;
    }
}

public class Timeline
{
    public Timeline? Next { get; set; }

    public float Time { get; set; }

    public Attach? Attach { get; set; }
    public bool IsKeyframeMix { get; private set; }

    [JsonProperty]
    private int keyframe_mix
    {
        set => IsKeyframeMix = value > 0;
    }

    public Matrix AnimationMatrix { get; set; } = Utility.Matrix.IdentityMatrixBy4X4;
    private float[]? _matrix { get; set; }

    [JsonProperty]
    private float[]? Matrix
    {
        set
        {
            if (value is null) return;
            AnimationMatrix = new Matrix(4, 4, value);
            _matrix = value;
        }
    }

    public bool IsMatrixMix { get; private set; }

    [JsonProperty]
    private int matrix_mix
    {
        set => IsMatrixMix = value > 0;
    }
}

public class Attach
{
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

    public AttachType AttachType { get; private set; }
    public Keyframe? Keyframe { get; set; }
    public Hitbox? Hitbox { get; set; }
    public int Id { get; set; }
}

public enum AttachType
{
    Keyframe,
    Slot,
    HitBox,
    Animation,
    Skeleton
}

[JsonConverter(typeof(SkeletonJsonConverter))]
public class QuadSkeleton
{
    public string Name { get; set; }
    public List<QuadBone>? Bone { get; set; }
}

public class QuadBone
{
    public Attach Attach { get; set; }
}