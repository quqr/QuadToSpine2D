using QTSCore.JsonConverters;

namespace QTSCore.Data.Spine;

[JsonConverter(typeof(SkinDeformConverter))]
public class Deform
{
    // {skinName:{slotName:{value}}}
    public Dictionary<string, Dictionary<string, AnimationDefault>> SkinName { get; set; } = new();

    public Deform Clone()
    {
        return new Deform
        {
            SkinName = new Dictionary<string, Dictionary<string, AnimationDefault>>(SkinName)
        };
    }
}
