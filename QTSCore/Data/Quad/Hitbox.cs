using QTSCore.JsonConverters;

namespace QTSCore.Data.Quad;

[JsonConverter(typeof(HitboxJsonConverter))]
public class Hitbox : Attach
{
    public string Name { get; set; } = string.Empty;
    public HitboxLayer[] Layer { get; set; }=[];
}
