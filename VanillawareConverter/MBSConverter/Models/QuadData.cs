using Newtonsoft.Json;

namespace VanillawareConverter.Mbs.Models;

public class QuadData
{
    [JsonProperty("blend")] public List<BlendMode?> Blend { get; set; } = [];

    [JsonProperty("mix")] public List<string?> Mix { get; set; } = [];

    [JsonProperty("keyframe")] public List<KeyframeData?> Keyframe { get; set; } = [];

    [JsonProperty("hitbox")] public List<HitboxData?> Hitbox { get; set; } = [];

    [JsonProperty("slot")] public List<SlotData?> Slot { get; set; } = [];

    [JsonProperty("animation")] public List<AnimationData?> Animation { get; set; } = [];

    [JsonProperty("skeleton")] public List<SkeletonData?> Skeleton { get; set; } = [];
}

public class BlendMode
{
    [JsonProperty("debug")] public string? Debug { get; set; }
}

public class KeyframeLayer
{
    [JsonProperty("debug")] public object[]? Debug { get; set; }

    [JsonProperty("dstquad")] public float[]? DstQuad { get; set; }

    [JsonProperty("blend_id")] public int BlendId { get; set; }

    [JsonProperty("fogquad")] public object? FogQuad { get; set; }

    [JsonProperty("tex_id")] public int? TexId { get; set; }

    [JsonProperty("srcquad")] public float[]? SrcQuad { get; set; }

    [JsonProperty("attribute")] public int? Attribute { get; set; }

    [JsonProperty("colorize")] public string? Colorize { get; set; }
}

public class KeyframeData
{
    [JsonProperty("debug")] public string? Debug { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("layer")] public List<KeyframeLayer?>? Layer { get; set; }
}

public class HitboxLayer
{
    [JsonProperty("debug")] public string? Debug { get; set; }

    [JsonProperty("hitquad")] public float[]? HitQuad { get; set; }
}

public class HitboxData
{
    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("layer")] public List<HitboxLayer?>? Layer { get; set; }
}

public class SlotData
{
    [JsonProperty("attach")] public List<object>? Attach { get; set; }
}

public class TimelineEntry
{
    [JsonProperty("debug")] public string? Debug { get; set; }

    [JsonProperty("time")] public int Time { get; set; }

    [JsonProperty("matrix_mix_id")] public int MatrixMixId { get; set; }

    [JsonProperty("color_mix_id")] public int ColorMixId { get; set; }

    [JsonProperty("dstquad_mix_id")] public int DstquadMixId { get; set; }

    [JsonProperty("fogquad_mix_id")] public int FogquadMixId { get; set; }

    [JsonProperty("hitquad_mix_id")] public int HitquadMixId { get; set; }

    [JsonProperty("attach")] public object? Attach { get; set; }

    [JsonProperty("color")] public string? Color { get; set; }

    [JsonProperty("matrix")] public double[]? Matrix { get; set; }
}

public class AnimationData
{
    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("timeline")] public List<TimelineEntry>? Timeline { get; set; }

    [JsonProperty("loop_id")] public int LoopId { get; set; }
}

public class BoneData
{
    [JsonProperty("attach")] public object? Attach { get; set; }
}

public class SkeletonData
{
    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("bone")] public List<BoneData?>? Bone { get; set; }
}