namespace QTSCore.Data.Spine;

public class DrawOrderOffset
{
    [JsonIgnore] public int SlotNum { get; set; }
    public string Slot { get; set; } = string.Empty;
    public int Offset { get; set; }
}
