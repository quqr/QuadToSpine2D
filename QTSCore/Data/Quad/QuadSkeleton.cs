using QTSCore.JsonConverters;

namespace QTSCore.Data.Quad;

[JsonConverter(typeof(SkeletonJsonConverter))]
public class QuadSkeleton : Attach
{
    public string Name { get; set; } = string.Empty;
    public QuadBone[]? Bone { get; set; } = [];
    [JsonIgnore] public AnimationData CombineAnimation { get; set; } = new();
}