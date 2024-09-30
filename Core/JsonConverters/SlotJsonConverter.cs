using Newtonsoft.Json.Linq;

namespace QuadToSpine2D.Core.JsonConverters;

public class SlotJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer                          serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj is not JArray jArray) return null;
        var slot = new Slot
        {
            Attaches = jArray.ToObject<List<Attach>>()
        };
        return slot;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Slot) == objectType;
    }
}