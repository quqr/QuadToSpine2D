using QuadToSpine2D.Core.JsonConverters;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Data.Quad;

public class QuadJson
{
    public List<Keyframe?> Keyframe { get; set; }
    public List<Animation?> Animation { get; set; }
    public List<QuadSkeleton?> Skeleton { get; set; }
    public List<Slot> Slot { get; set; }
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
    private float[]? _dstquad = new float[8];

    public float[]? Dstquad
    {
        get => _dstquad;
        set
        {
            //Y is down
            if (value is null) _dstquad = value;
            else
                for (var i = 0; i < 8; i++)
                {
                    if (i % 2 != 0)
                    {
                        _dstquad[i] = -value[i];
                        continue;
                    }

                    _dstquad[i] = value[i];
                }
        }
    }

    private float[]? _srcquad;

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            LayerGuid = string.Empty;
            if (_srcquad is null) return;
            MinAndMaxSrcPoints = ProcessUtility.FindMinAndMaxPoints(_srcquad);
            Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
            Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
            LayerGuid = $"{TexId}_{_srcquad
                .Select((t, i) => t * 3.7 / 7.3 + t * i * 97311397.135f / 773377.2746f)
                .Sum()}";
            CalculateUVs(_srcquad);
        }
    }

    public int BlendId { get; set; }
    public int TexId { get; set; }
    public string LayerGuid { get; set; } = "";
    public float Height { get; set; }
    public float Width { get; set; }
    public float[] MinAndMaxSrcPoints { get; set; }
    public float[] UVs { get; set; } = new float[8];
    public float[] ZeroCenterPoints { get; set; } = new float[8];
    public string LayerName { get; set; }

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
    public List<Timeline> Timeline { get; set; }
}

public class Timeline
{
    public int Time { get; set; }
    public Attach? Attach { get; set; }
    [JsonIgnore]
    public bool IsKeyframeMix { get; private set; }
    [JsonProperty]
    private int KeyframeMix
    {
        set => IsKeyframeMix = value > 0;
    }
}

public class Attach
{
    public string Type { get; set; }
    public int Id { get; set; }
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