using Newtonsoft.Json.Linq;
using QTSCore.Data.Quad;

namespace QTSCore.JsonConverters;

public class SlotJsonConverter : JsonConverter
{
    public static int IdCount = -1;


    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        IdCount++;
        var obj = serializer.Deserialize(reader);
        if (obj is not JArray jArray) return null;
        var slot = new Slot
        {
            Attaches = jArray.ToObject<List<Attach>>(), Id = IdCount, AttachType = AttachType.Slot
        };
        return slot;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Slot) == objectType;
    }
}