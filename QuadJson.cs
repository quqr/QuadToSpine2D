using System.Numerics;
using Newtonsoft.Json.Linq;

namespace QuadPlayer;

public class QuadJson
{
    public List<Keyframe?> Keyframe { get; set; }
    public List<Animation> Animation { get; set; }
    public List<Skeleton> Skeleton { get; set; }
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
    public float Width{ get; set; }
    public float Height{ get; set; }
    public Vector2 CenterPoint { get; set; }=Vector2.Zero;
    public void CalculateRect(float[] points)
    {
        Width = 4 * (Math.Max(Math.Abs(points[2]), Math.Abs(points[0])) -
                     Math.Min(Math.Abs(points[2]), Math.Abs(points[0])));
        Height = 4 * (Math.Max(Math.Abs(points[3]), Math.Abs(points[1])) -
                      Math.Min(Math.Abs(points[1]), Math.Abs(points[3])));
        CenterPoint = new Vector2(points[0] + Width / 2, points[1] + Height / 2);
    }
}

public class KeyframeJsonConverter: JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj.GetType()!=typeof(JObject)) return null;
        var jobj = obj as JObject;
        var keyframe = new Keyframe();
        keyframe.Name = jobj["name"]?.ToString();
        keyframe.Layer = jobj["layer"]?.ToObject<List<KeyframeLayer?>>();
        return keyframe;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Keyframe) == objectType;
    }
}

public class KeyframeLayerJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj.GetType()!=typeof(JObject)) return null;
        var jobj = obj as JObject;
        var layer = new KeyframeLayer();
        layer.Dstquad = jobj["dstquad"]?.ToObject<float[]>();
        layer.BlendId = jobj["blend_id"]?.ToObject<int>();
        layer.Attribute = jobj["attribute"]?.ToString();
        layer.Colorize = jobj["colorize"]?.ToString();
        layer.TexId = jobj["tex_id"]?.ToObject<int>();
        layer.Srcquad = jobj["srcquad"]?.ToObject<float[]>();
        return layer;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}

[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer
{
    private float[]? _dstquad;

    public float[]? Dstquad
    {
        get => _dstquad;
        set
        {
            _dstquad = value;
            if(_dstquad is null) return;
            MinAndMaxDstPoints = ProcessTools.FindMinAndMaxPoints(_dstquad);
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
            if(_srcquad is null) return;
            MinAndMaxSrcPoints = ProcessTools.FindMinAndMaxPoints(_srcquad);
            Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
            Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
            LayerGuid = _srcquad.Sum(x => x / 7).ToString();
        }
    }

    public Vector2 CenterPosition { get; set; }
    public int? BlendId{ get; set; }
    public string? Attribute{ get; set; }
    public string? Colorize { get; set; }
    public int? TexId{ get; set; }
    public string LayerGuid { get; set; } = "None";
    public float Height{ get; set; }
    public float Width{ get; set; }
    public float Rotate{ get; set; }
    public float[] MinAndMaxDstPoints{ get; set; }
    public float[] MinAndMaxSrcPoints{ get; set; }

    public void CalculateRotate()
    {
        if (Dstquad is null || Srcquad is null) return;
        var dst = new Vector2(MinAndMaxDstPoints[2] - MinAndMaxDstPoints[0],
            MinAndMaxDstPoints[3] - MinAndMaxDstPoints[1]);
        var src = new Vector2(MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0],
            MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1]);
        var dot = (src.X * dst.X - src.Y * dst.Y);
        Rotate = (src.X * dst.X + src.Y * dst.Y) / (dst.Length() * src.Length()) *
                 (dot / float.Abs(dot));
        CenterPosition = new Vector2(MinAndMaxDstPoints[0] + Width / 2, MinAndMaxDstPoints[1] + Height / 2);
    }
}

public class Animation
{
    public string Name { get; set; }
    public List<Timeline> Timeline{ get; set; }
    public int LoopId { get; set; }
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

public class SkeletonJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj.GetType()!=typeof(JObject)) return null;
        var jobj = obj as JObject;
        var skeleton = new Skeleton();
        skeleton.Name = jobj["name"]?.ToString();
        skeleton.Bone = jobj["bone"]?.ToObject<List<Bone>>();
        return skeleton;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Skeleton) == objectType;
    }
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
