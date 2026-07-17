using QTSCore.JsonConverters;

namespace QTSCore.Data.Quad;

[JsonConverter(typeof(KeyframeJsonConverter))]
public class Keyframe : Attach
{
    public string Name { get; set; } = string.Empty;

    public KeyframeLayer?[]? Layers { get; set; } = [];
    public int[] Order { get; set; } = [];
}