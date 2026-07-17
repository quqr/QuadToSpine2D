using QTSCore.JsonConverters;

namespace QTSCore.Data.Spine;

[JsonConverter(typeof(AnimationDefaultJsonConverter))]
public class AnimationDefault
{
    // mesh name
    public string Name { get; set; }
    public List<AnimationVertices> ImageVertices { get; set; } = [];
}