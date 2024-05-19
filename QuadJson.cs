using System.Numerics;

namespace QuadPlayer;

public class QuadJson
{
    public string Tag { get; set; }
    public List<Keyframe> Keyframe { get; set; }
    public List<Hitbox> Hitbox{ get; set; }
    public List<Animation> Animation { get; set; }
    public List<Skeleton> Skeleton { get; set; }
    public List<Slot> Slot { get; set; }
    public List<Blend> Blend { get; set; }
    public List<Link> Link { get; set; }
}

public class Keyframe
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public List<KeyframeLayer> Layers{ get; set; }
    public int Order{ get; set; }
    public float Width;
    public float Height;
    public float[] CenterPoint = new float[2];
    public void CalculateRect(float[] points)
    {
        Width = (Math.Abs(points[2]) + Math.Abs(points[0])) * 4;
        Height = (Math.Abs(points[3]) + Math.Abs(points[1])) * 4;
        CenterPoint[0] = points[0] + Width / 2;
        CenterPoint[1] = points[1] + Height / 2;
    }
}

public class KeyframeLayer
{
    public List<string> Debug{ get; set; }
    private float[]? _dstquad { get; set; }
    public float[]? Dstquad
    {
        get => _dstquad;
        set
        {
            _dstquad = value;
            DstPoints = ProcessTools.FindMinAndMaxPoints(_dstquad);
        }
    }
    public int BlendID{ get; set; }
    public string Fogquad { get; set; }
    public string Attribute{ get; set; }
    public string Colorize { get; set; }
    public int TexID{ get; set; }
    public string LayerGUID { get; set; }
    private float[]? _srcquad { get; set; }
    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            var xy = ProcessTools.FindMinAndMaxPoints(value);
            Height = xy[2] - xy[0];
            Width = xy[3] - xy[1];
            SrcPoints = xy;
        }
    }
    public float Height{ get; set; }
    public float Width{ get; set; }
    public float Rotate{ get; set; }
    public float[] DstPoints{ get; set; }
    public float[] SrcPoints{ get; set; }

    public void CalculateRotate()
    {
        if (Dstquad is null || Srcquad is null) return;
        Vector2 dst = new Vector2(DstPoints[2] - DstPoints[0], DstPoints[3] - DstPoints[1]);
        Vector2 src = new Vector2(SrcPoints[2] - SrcPoints[0], SrcPoints[3] - SrcPoints[1]);
        var dot = (src.X * dst.X - src.Y * dst.Y);
        Rotate = (src.X * dst.X + src.Y * dst.Y) / (dst.Length() * src.Length()) *
                 (dot / float.Abs(dot));
    }
}


public class Hitbox
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public List<HitBoxLayer> Layer = [];
}

public class HitBoxLayer
{
    public string Debug { get; set; }
    public List<int> Hitquad = [];
    public List<string> Attribute = [];
}

public class Animation
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public List<Timeline> Timeline= [];
    public int? LoopId { get; set; }
}

public class Timeline
{
    public string Debug { get; set; }
    public int Time { get; set; }
    public Attach Attach { get; set; }
    public float[] Matrix = new float[16];
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

public class Skeleton
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public List<Bone> Bone = [];
}

public class Bone
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public Attach Attach { get; set; }
}

public class Slot
{
    public string Type { get; set; }
    public int Id { get; set; }
}

public class Blend
{
    public string Debug { get; set; }
    public string Name { get; set; }
    public List<string> Mode = [];
    public string Color { get; set; }
}

public class Link
{
    public string List { get; set; }
    public int Id { get; set; }
}

