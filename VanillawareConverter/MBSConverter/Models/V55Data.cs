namespace VanillawareConverter.Mbs.Models;

public class V55Data
{
    public List<S0Color?> S0 { get; set; } = [];
    public List<S1Source?> S1 { get; set; } = [];
    public List<S2Dest?> S2 { get; set; } = [];
    public List<S3Hitbox?> S3 { get; set; } = [];
    public List<S4Texture?> S4 { get; set; } = [];
    public List<S5HitboxRef?> S5 { get; set; } = [];
    public List<S6Keyframe?> S6 { get; set; } = [];
    public List<S7Transform?> S7 { get; set; } = [];
    public List<S8AnimFrame?> S8 { get; set; } = [];
    public List<S9Bone?> S9 { get; set; } = [];
    public List<SaAnimation?> Sa { get; set; } = [];
    public List<SbExtension?> Sb { get; set; } = [];

    public string Tag { get; set; } = string.Empty;
    public string Id3 { get; set; } = string.Empty;
    public string Ver { get; set; } = "55";
}

public class S0Color
{
    public string I { get; set; } = string.Empty;
    public object? Fog { get; set; }
}

public class S1Source
{
    public string I { get; set; } = string.Empty;
    public float[]? SrcQuad { get; set; }
}

public class S2Dest
{
    public string I { get; set; } = string.Empty;
    public float[]? DstQuad { get; set; }
}

public class S3Hitbox
{
    public string I { get; set; } = string.Empty;
    public float[]? Rect { get; set; }
    public float[]? Xyz { get; set; }
}

public class S4Texture
{
    public string I { get; set; } = string.Empty;
    public int Blend { get; set; }
    public int Tex { get; set; }
    public int[] S0S1S2 { get; set; } = new int[3];
    public string Bits { get; set; } = "0x0";
    public string Attr { get; set; } = "0x0";
    public int Color { get; set; }
}

public class S5HitboxRef
{
    public string I { get; set; } = string.Empty;
    public int S3 { get; set; }
    public string Bits { get; set; } = "0x0";
}

public class S6Keyframe
{
    public string I { get; set; } = string.Empty;
    public float[]? Rect { get; set; }
    public int[] S4 { get; set; } = new int[2];
    public int[] S5 { get; set; } = new int[2];
    public string Bits { get; set; } = "0x0";
}

public class S7Transform
{
    public string I { get; set; } = string.Empty;
    public float[] Move { get; set; } = new float[3];
    public float[] Rotate { get; set; } = new float[3];
    public float[] Scale { get; set; } = new float[2];
    public string Fog { get; set; } = "#ffffffff";
}

public class S8AnimFrame
{
    public string I { get; set; } = string.Empty;
    public int S6 { get; set; }
    public int S7 { get; set; }
    public int Time { get; set; }
    public int Loop { get; set; }
    public int Sfx { get; set; }
    public string Bits { get; set; } = "0x0";
    public int InS5S3 { get; set; } = -1;
    public int InS7 { get; set; } = -1;
    public int InS6 { get; set; } = -1;
    public int InS0S1S2 { get; set; } = -1;
}

public class S9Bone
{
    public string I { get; set; } = string.Empty;
    public float[]? Rect { get; set; }
    public string Name { get; set; } = string.Empty;
    public int[] Sa { get; set; } = new int[2];
}

public class SaAnimation
{
    public string I { get; set; } = string.Empty;
    public int[] S8 { get; set; } = new int[3];
}

public class SbExtension
{
    public string I { get; set; } = string.Empty;
}