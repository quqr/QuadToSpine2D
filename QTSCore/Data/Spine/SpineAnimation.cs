namespace QTSCore.Data.Spine;

public class SpineAnimation
{
    //[JsonIgnore]
    public Dictionary<string, AnimationSlot> Slots { get; set; } = [];

    //[JsonIgnore]
    public Deform Deform { get; set; } = new();

    //[JsonIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<DrawOrder>? DrawOrder { get; set; }
}