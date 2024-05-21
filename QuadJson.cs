using System.Numerics;
using Newtonsoft.Json.Linq;
using QuadPlayer.JsonConverters;

namespace QuadPlayer;

public class QuadJson
{
    public List<Keyframe?> Keyframe { get; set; }
    public List<Animation?> Animation { get; set; }
    public List<Skeleton?> Skeleton { get; set; }
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
            ID = Convert.ToInt32(_name.Split(' ').Last());
        }
    }

    public int ID { get; set; }
    public List<KeyframeLayer?>? Layer;
}



[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer
{
    public float[]? Dstquad { get; set; }

    private float[]? _srcquad;

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            LayerGuid = string.Empty;
            if(_srcquad is null) return;
            MinAndMaxSrcPoints = ProcessTools.FindMinAndMaxPoints(_srcquad);
            Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
            Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
            LayerGuid = _srcquad.Sum(x => x / 7).ToString();
            CalculateUVs(_srcquad);
        }
    }
    public int? BlendId{ get; set; }
    public string? Attribute{ get; set; }
    public string? Colorize { get; set; }
    public int? TexId{ get; set; }
    public string LayerGuid { get; set; } = "";
    public float Height{ get; set; }
    public float Width{ get; set; }
    public float[] MinAndMaxSrcPoints{ get; set; }
    public float[] UVs { get; set; } = new float[8];

    void CalculateUVs(float[] src)
    {
        List<Vector3> points =
        [
            new Vector3(src[0], src[1], 0),
            new Vector3(src[2], src[3], 1),
            new Vector3(src[4], src[5], 2),
            new Vector3(src[6], src[7], 3)
        ];
        Vector2[] uvs = [new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1)];
        var orderPoints = points.OrderBy(a=>a.X).ThenBy(b=>b.Y).ToList();
        for (int i = 0; i < 4; i++) {
            UVs[(int)orderPoints[i].Z * 2] = uvs[i].X;
            UVs[(int)orderPoints[i].Z * 2 + 1] = uvs[i].Y;
        }
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
            if(_name.Equals("ALL KEYFRAMES")) return;
            ID = Convert.ToInt32(_name.Split(' ').Last());
        }
    }

    public int ID { get; set; }
    public List<Timeline> Timeline{ get; set; }
    public int LoopID { get; set; }
}

public class Timeline
{
    public int Time { get; set; }
    public Attach Attach { get; set; }
    public float[]? Matrix { get; set; }
    public string Color { get; set; }
    public bool MatrixMix { get; set; }
    public bool ColorMix { get; set; }
    public bool KeyframeMix { get; set; }
    public bool HitboxMix { get; set; }
}

public class Attach
{
    public string Type { get; set; }
    public int ID { get; set; }
}


[JsonConverter(typeof(SkeletonJsonConverter))]
public class Skeleton
{
    public string Name { get; set; }
    public List<Bone>? Bone { get; set; }
}

public class Bone
{
    public string Name { get; set; }
    public Attach Attach { get; set; }
}
