namespace QTSCore.Data.Spine;

public class SpineSlot
{
    public string Name { get; set; }
    public string Bone { get; set; } = "root";
    public string Blend { get; set; }
    [JsonIgnore] public string Attachment { get; set; }
    [JsonIgnore] public int SlotOrder { get; set; }
    [JsonIgnore] public int OrderByImageSlot { get; set; }
}
