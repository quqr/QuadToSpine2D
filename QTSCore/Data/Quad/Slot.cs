using QTSCore.JsonConverters;

namespace QTSCore.Data.Quad;

[JsonConverter(typeof(SlotJsonConverter))]
public class Slot : Attach
{
    public Attach[]? Attaches { get; set; }
}